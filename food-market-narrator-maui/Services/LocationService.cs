using Microsoft.Maui.Devices.Sensors;

namespace food_market_narrator.Services;

public class LocationService : ILocationService
{
    private bool _isTracking = false;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly GeolocationRequest TrackingRequest =
        new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

    public event EventHandler<Location>? LocationChanged;

    // Lấy vị trí hiện tại của người dùng
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
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting location: {ex.Message}");
            return null;
        }
    }

    public async Task StartTrackingAsync()
    {
        if (_isTracking) return;

        var status = await CheckAndRequestPermissionAsync();
        if (status != PermissionStatus.Granted)
        {
            Console.WriteLine("Location permission not granted");
            return;
        }

        try
        {
            _isTracking = true;
            _trackingCts = new CancellationTokenSource();
            _trackingTask = RunTrackingLoopAsync(_trackingCts.Token);
            Console.WriteLine("Bắt đầu theo dõi vị trí");
        }
        catch (Exception ex)
        {
            _isTracking = false;
            Console.WriteLine($"Error starting tracking: {ex.Message}");
        }
    }

    public void StopTracking()
    {
        if (!_isTracking) return;

        try
        {
            _trackingCts?.Cancel();
            _isTracking = false;
            Console.WriteLine("Ngừng theo dõi vị trí");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping tracking: {ex.Message}");
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
                    LocationChanged?.Invoke(this, location);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Tracking loop error: {ex.Message}");
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

    private async Task<PermissionStatus> CheckAndRequestPermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (status == PermissionStatus.Granted)
            return status;

        if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
        {
            // Hiển thị cho người dùng biết thêm thông tin về lý do cần quyền truy cập
        }

        return await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
    }
}