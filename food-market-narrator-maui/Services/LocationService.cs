using Microsoft.Maui.Devices.Sensors;

namespace food_market_narrator.Services;

public class LocationService : ILocationService
{
    private bool _isTracking = false;
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
            
            // Thiết lập lắng nghe liên tục
            Geolocation.Default.LocationChanged += OnGeolocationLocationChanged;
            Geolocation.Default.ListeningFailed += OnGeolocationListeningFailed;

            var request = new GeolocationListeningRequest(GeolocationAccuracy.Best)
            {
                MinimumTime = TimeSpan.FromSeconds(3), // Tần suất cập nhật
                // MinimumDistance = 1, // Khoảng cách tối thiểu để nhận cập nhật
                DesiredAccuracy = GeolocationAccuracy.High
            };

            await Geolocation.Default.StartListeningForegroundAsync(request);
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
            Geolocation.Default.StopListeningForeground();
            Geolocation.Default.LocationChanged -= OnGeolocationLocationChanged;
            Geolocation.Default.ListeningFailed -= OnGeolocationListeningFailed;
            _isTracking = false;
            Console.WriteLine("Ngừng theo dõi vị trí");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error stopping tracking: {ex.Message}");
        }
    }

    private void OnGeolocationLocationChanged(object? sender, GeolocationLocationChangedEventArgs e)
    {
        LocationChanged?.Invoke(this, e.Location);
    }

    private void OnGeolocationListeningFailed(object? sender, GeolocationListeningFailedEventArgs e)
    {
        Console.WriteLine($"Location listening failed: {e.Error}");
        _isTracking = false; // Hoặc logic thử lại
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