using food_market_narrator.Settings;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;
using System.Text.Json;

namespace food_market_narrator.Services;

// Service gom và đồng bộ location logs theo batch định kỳ lên backend.
public class LocationLogSyncService : ILocationLogSyncService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
    private const int MaxBufferSize = 2000;
    private const string DeviceIdPreferenceKey = "tracking_device_id";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly object _bufferLock = new();
    private readonly List<LocationLogItem> _buffer = [];
    private readonly HttpClient _httpClient;
    private readonly ILocationService _locationService;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");
    private readonly SemaphoreSlim _bufferFileLock = new(1, 1);
    public string CurrentSessionId => _sessionId;

    private CancellationTokenSource? _flushCts;
    private Task? _flushTask;
    private bool _started;
    private bool _sessionStartedSynced;

    public LocationLogSyncService(HttpClient httpClient, ILocationService locationService)
    {
        _httpClient = httpClient;
        _locationService = locationService;
    }

    // Khởi động cơ chế ghi nhận location sample và vòng lặp flush định kỳ.
    public void Start()
    {
        if (_started)
        {
            return;
        }

        LoadBufferFromDisk();

        _started = true;
        _locationService.LocationSampled += OnLocationSampled;
        _ = EnsureSessionStartedAsync(CancellationToken.None);

        _flushCts = new CancellationTokenSource();
        _flushTask = RunFlushLoopAsync(_flushCts.Token);
    }

    // Cho phép trigger flush thủ công (ví dụ trước khi gửi audio log cần session chắc chắn).
    public Task FlushNowAsync()
    {
        return FlushOnceAsync(CancellationToken.None);
    }

    // Nhận mẫu vị trí từ LocationService và thêm vào buffer trong bộ nhớ.
    private void OnLocationSampled(object? sender, Location? location)
    {
        var item = new LocationLogItem
        {
            SessionId = _sessionId,
            Timestamp = DateTime.UtcNow,
            Location = location == null
                ? null
                : new GeoPointPayload
                {
                    Type = "Point",
                    Coordinates = [location.Longitude, location.Latitude]
                }
        };

        lock (_bufferLock)
        {
            _buffer.Add(item);

            // Keep memory bounded if network is unstable for a long period.
            if (_buffer.Count > MaxBufferSize)
            {
                var removeCount = _buffer.Count - MaxBufferSize;
                _buffer.RemoveRange(0, removeCount);
            }
        }

        _ = PersistBufferSnapshotAsync();
    }

    // Vòng lặp nền: cứ mỗi FlushInterval sẽ thử đẩy batch hiện có lên server.
    private async Task RunFlushLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(FlushInterval);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var hasNext = await timer.WaitForNextTickAsync(cancellationToken);
                if (!hasNext)
                {
                    break;
                }

                await FlushOnceAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception)
            {
                // Keep loop alive; failed batches are restored to memory for retry.
            }
        }
    }

    // Flush một lần: gửi batch hiện tại, nếu fail thì đưa lại buffer để retry lần sau.
    private async Task FlushOnceAsync(CancellationToken cancellationToken)
    {
        await EnsureSessionStartedAsync(cancellationToken);

        List<LocationLogItem> batch;
        lock (_bufferLock)
        {
            if (_buffer.Count == 0)
            {
                return;
            }

            batch = [.. _buffer];
            _buffer.Clear();
        }

        var request = new LocationLogBatchRequest
        {
            Items = batch
        };

        var firstPoiCapturedAtUtc = batch.Min(item => item.Timestamp);
        var sendLatLngAtUtc = DateTime.UtcNow;

        Console.WriteLine(
            $"Sync log to server: sending {batch.Count} location points | firstPoiCapturedAtUtc={firstPoiCapturedAtUtc:O} | sendLatLngAtUtc={sendLatLngAtUtc:O}");

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                AppSettings.LocationLogsBatchEndpoint,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine(
                    $"Sync log to server: sent {batch.Count} location points successfully | firstPoiCapturedAtUtc={firstPoiCapturedAtUtc:O} | sendLatLngAtUtc={sendLatLngAtUtc:O}");
                _ = PersistBufferSnapshotAsync();
                return;
            }

            Console.WriteLine(
                $"Sync log to server: failed with status {(int)response.StatusCode} | firstPoiCapturedAtUtc={firstPoiCapturedAtUtc:O} | sendLatLngAtUtc={sendLatLngAtUtc:O}");
        }
        catch (Exception)
        {
            // Restore for retry on next flush tick.
            Console.WriteLine(
                $"Sync log to server: exception while sending {batch.Count} location points | firstPoiCapturedAtUtc={firstPoiCapturedAtUtc:O} | sendLatLngAtUtc={sendLatLngAtUtc:O}");
        }

        lock (_bufferLock)
        {
            _buffer.InsertRange(0, batch);
            if (_buffer.Count > MaxBufferSize)
            {
                var removeCount = _buffer.Count - MaxBufferSize;
                _buffer.RemoveRange(0, removeCount);
            }
        }

        _ = PersistBufferSnapshotAsync();
    }

    // Đảm bảo session đã tồn tại trên backend trước khi gửi location logs.
    private async Task EnsureSessionStartedAsync(CancellationToken cancellationToken)
    {
        if (_sessionStartedSynced)
        {
            return;
        }

        var request = new UserSessionStartRequest
        {
            SessionId = _sessionId,
            DeviceId = GetOrCreateDeviceId(),
            DeviceInfo = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}, {DeviceInfo.Platform} {DeviceInfo.VersionString}"
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                AppSettings.UserSessionsStartEndpoint,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _sessionStartedSynced = true;
                return;
            }

            Console.WriteLine($"Sync session start: failed with status {(int)response.StatusCode}");
        }
        catch (Exception)
        {
            Console.WriteLine("Sync session start: exception while creating session");
        }
    }

    // Lấy hoặc sinh device id duy nhất để gắn với session theo dõi.
    private static string GetOrCreateDeviceId()
    {
        var existingDeviceId = Preferences.Get(DeviceIdPreferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existingDeviceId))
        {
            return existingDeviceId;
        }

        var generated = Guid.NewGuid().ToString("N");
        Preferences.Set(DeviceIdPreferenceKey, generated);
        return generated;
    }

    // Trả về đường dẫn file buffer location logs trên local storage.
    private static string GetLocationLogsBufferFilePath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "offline_cache");
        Directory.CreateDirectory(cacheDir);
        return Path.Combine(cacheDir, "location_logs_buffer.json");
    }

    // Khôi phục buffer từ disk khi app khởi động lại để không mất log chưa sync.
    private void LoadBufferFromDisk()
    {
        try
        {
            var filePath = GetLocationLogsBufferFilePath();
            if (!File.Exists(filePath))
            {
                return;
            }

            var json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                return;
            }

            var persisted = JsonSerializer.Deserialize<List<LocationLogItem>>(json, JsonOptions);
            if (persisted == null || persisted.Count == 0)
            {
                return;
            }

            // Re-bind unsent logs to current runtime session to keep backend contract consistent.
            foreach (var item in persisted)
            {
                item.SessionId = _sessionId;
            }

            lock (_bufferLock)
            {
                _buffer.Clear();
                _buffer.AddRange(persisted);
                if (_buffer.Count > MaxBufferSize)
                {
                    var removeCount = _buffer.Count - MaxBufferSize;
                    _buffer.RemoveRange(0, removeCount);
                }
            }

            Console.WriteLine($"Sync log to server: restored {_buffer.Count} unsent location points from disk");
        }
        catch (Exception)
        {
            Console.WriteLine("Sync log to server: failed to restore persisted location buffer");
        }
    }

    // Persist snapshot buffer hiện tại xuống disk (best-effort) để hỗ trợ retry sau crash/restart.
    private async Task PersistBufferSnapshotAsync()
    {
        List<LocationLogItem> snapshot;
        lock (_bufferLock)
        {
            snapshot = [.. _buffer];
        }

        var filePath = GetLocationLogsBufferFilePath();

        await _bufferFileLock.WaitAsync();
        try
        {
            if (snapshot.Count == 0)
            {
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                return;
            }

            var json = JsonSerializer.Serialize(snapshot, JsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
        catch (Exception)
        {
            Console.WriteLine("Sync log to server: failed to persist location buffer to disk");
        }
        finally
        {
            _bufferFileLock.Release();
        }
    }
}

public class LocationLogBatchRequest
{
    public List<LocationLogItem> Items { get; set; } = [];
}

public class LocationLogItem
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public GeoPointPayload? Location { get; set; }
}

public class GeoPointPayload
{
    public string Type { get; set; } = "Point";
    public List<double?> Coordinates { get; set; } = [];
}

public class UserSessionStartRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
}
