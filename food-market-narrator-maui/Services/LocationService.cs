using Microsoft.Maui.Devices.Sensors;

namespace food_market_narrator.Services;

public class LocationService : ILocationService
{
    private bool _isTracking = false;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;
    private Location? _lastPublishedLocation;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const double MinPublishDistanceMeters = 6;
    private static readonly GeolocationRequest TrackingRequest =
        new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

    public event EventHandler<Location>? LocationChanged;

    // Láº¥y vá»‹ trÃ­ hiá»‡n táº¡i cá»§a ngÆ°á»i dÃ¹ng
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var status = await CheckAndRequestPermissionAsync();
            if (status != PermissionStatus.Granted)
                return null;

            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            return await Geolocation.Default.GetLocationAsync(request);
        }
        catch (Exception)
        {
            // Console.WriteLine($"Error getting location: {ex.Message}");
            return null;
        }
    }

    public async Task StartTrackingAsync()
    {
        if (_isTracking) return;

        var status = await CheckAndRequestPermissionAsync();
        if (status != PermissionStatus.Granted)
        {
            // Console.WriteLine("Location permission not granted");
            return;
        }

        try
        {
            _isTracking = true;
            _trackingCts = new CancellationTokenSource();
            _trackingTask = RunTrackingLoopAsync(_trackingCts.Token);
            // Console.WriteLine("Báº¯t Ä‘áº§u theo dÃµi vá»‹ trÃ­");
        }
        catch (Exception)
        {
            _isTracking = false;
            // Console.WriteLine($"Error starting tracking: {ex.Message}");
        }
    }

    public void StopTracking()
    {
        if (!_isTracking) return;

        try
        {
            _trackingCts?.Cancel();
            _isTracking = false;
            // Console.WriteLine("Ngá»«ng theo dÃµi vá»‹ trÃ­");
        }
        catch (Exception)
        {
            // Console.WriteLine($"Error stopping tracking: {ex.Message}");
        }
        finally
        {
            _trackingCts?.Dispose();
            _trackingCts = null;
            _trackingTask = null;
        }
    }

    private async Task RunTrackingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var location = await Geolocation.Default.GetLocationAsync(TrackingRequest);
                if (location != null)
                {
                    if (ShouldPublish(location))
                    {
                        _lastPublishedLocation = location;
                        LocationChanged?.Invoke(this, location);
                    }
                }
            }
            catch (Exception)
            {
                // Console.WriteLine($"Tracking loop error: {ex.Message}");
            }

            try
            {
                await Task.Delay(PollInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private bool ShouldPublish(Location location)
    {
        if (_lastPublishedLocation == null)
        {
            return true;
        }

        var distanceMeters = Location.CalculateDistance(
            _lastPublishedLocation,
            location,
            DistanceUnits.Kilometers) * 1000;

        return distanceMeters >= MinPublishDistanceMeters;
    }

    private async Task<PermissionStatus> CheckAndRequestPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
            return status;

        if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
        {
            // Hiá»ƒn thá»‹ cho ngÆ°á»i dÃ¹ng biáº¿t thÃªm thÃ´ng tin vá» lÃ½ do cáº§n quyá»n truy cáº­p
        }

        return await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }
}

