using food_market_narrator.Settings;
using Microsoft.Maui.Devices.Sensors;
using System.Net.Http.Json;

namespace food_market_narrator.Services;

public class LocationLogSyncService : ILocationLogSyncService
{
    private static readonly TimeSpan FlushInterval = TimeSpan.FromSeconds(10);
    private const int MaxBufferSize = 2000;

    private readonly object _bufferLock = new();
    private readonly List<LocationLogItem> _buffer = [];
    private readonly HttpClient _httpClient;
    private readonly ILocationService _locationService;
    private readonly string _sessionId = Guid.NewGuid().ToString("N");

    private CancellationTokenSource? _flushCts;
    private Task? _flushTask;
    private bool _started;

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

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                AppSettings.LocationLogsBatchEndpoint,
                request,
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return;
            }
        }
        catch (Exception)
        {
            // Restore for retry on next flush tick.
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
