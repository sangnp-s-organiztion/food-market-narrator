using food_market_narrator.Settings;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Storage;
using System.Net.Http.Json;

namespace food_market_narrator.Services;

public class LocationLogSyncService : ILocationLogSyncService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
    private const int MaxBufferSize = 2000;
    private const string DeviceIdPreferenceKey = "tracking_device_id";

    private readonly object _bufferLock = new();
    private readonly List<LocationLogItem> _buffer = [];
    private readonly HttpClient _httpClient;
    private readonly ILocationService _locationService;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    private CancellationTokenSource? _flushCts;
    private Task? _flushTask;
    private bool _started;
    private bool _sessionStartedSynced;

    public LocationLogSyncService(HttpClient httpClient, ILocationService locationService)
    {
        _httpClient = httpClient;
        _locationService = locationService;
    }

    public void Start()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _locationService.LocationSampled += OnLocationSampled;
        _ = EnsureSessionStartedAsync(CancellationToken.None);

        _flushCts = new CancellationTokenSource();
        _flushTask = RunFlushLoopAsync(_flushCts.Token);
    }

    public Task FlushNowAsync()
    {
        return FlushOnceAsync(CancellationToken.None);
    }

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
    }

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

        Console.WriteLine($"Sync log to server: sending {batch.Count} location points");

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                AppSettings.LocationLogsBatchEndpoint,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Sync log to server: sent {batch.Count} location points successfully");
                return;
            }

            Console.WriteLine($"Sync log to server: failed with status {(int)response.StatusCode}");
        }
        catch (Exception)
        {
            // Restore for retry on next flush tick.
            Console.WriteLine($"Sync log to server: exception while sending {batch.Count} location points");
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
    }

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
