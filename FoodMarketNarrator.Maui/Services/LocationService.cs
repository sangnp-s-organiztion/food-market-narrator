using Microsoft.Maui.Devices.Sensors;
using System.Linq;

namespace food_market_narrator.Services;

// Service theo dõi GPS: quản lý quyền, vòng lặp polling và publish event vị trí đã lọc.
public class LocationService : ILocationService
{
    private bool _isTracking = false;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;
    private Location? _lastKnownLocation;
    private Location? _lastPublishedLocation;
    private readonly SemaphoreSlim _trackingInitLock = new(1, 1);
    private readonly SemaphoreSlim _permissionLock = new(1, 1);

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const double MinPublishDistanceMeters = 6;
    private static readonly GeolocationRequest TrackingRequest =
        new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
    private bool _backgroundPermissionExplained;

    public event EventHandler<Location>? LocationChanged;
    public event EventHandler<Location?>? LocationSampled;
    public Location? LastKnownLocation => _lastKnownLocation;

    // Lấy vị trí hiện tại một lần, có xin quyền foreground nếu cần.
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var granted = await EnsureForegroundTrackingPermissionAsync();
            if (!granted)
                return null;

            var request = new GeolocationRequest(GeolocationAccuracy.High, TimeSpan.FromSeconds(10));
            var location = await Geolocation.Default.GetLocationAsync(request);
            if (location != null)
            {
                _lastKnownLocation = location;
            }

            return location;
        }
        catch (Exception)
        {
            // Console.WriteLine($"Error getting location: {ex.Message}");
            return null;
        }
    }

    // Bắt đầu tracking nền theo PollInterval và phát event khi vị trí thay đổi đủ ngưỡng.
    public async Task StartTrackingAsync()
    {
        if (_isTracking)
        {
            return;
        }

        await _trackingInitLock.WaitAsync();
        try
        {
            if (_isTracking)
            {
                return;
            }

            var granted = await EnsureForegroundTrackingPermissionAsync();
            if (granted)
            {
                StartForegroundTrackingServiceIfNeeded();
            }

            _isTracking = true;
            _trackingCts = new CancellationTokenSource();
            _trackingTask = RunTrackingLoopAsync(_trackingCts.Token);
            // Console.WriteLine("Started location tracking");
        }
        catch (Exception)
        {
            _isTracking = false;
            // Console.WriteLine($"Error starting tracking: {ex.Message}");
        }
        finally
        {
            _trackingInitLock.Release();
        }
    }

    // Yêu cầu quyền background location trên Android 10+ khi tính năng cần theo dõi nền.
    public async Task<bool> RequestBackgroundLocationPermissionAsync()
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            return true;
        }

        var foregroundGranted = await EnsureForegroundTrackingPermissionAsync();
        if (!foregroundGranted)
        {
            return false;
        }

        var alwaysStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (alwaysStatus == PermissionStatus.Granted)
        {
            return true;
        }

        if (!_backgroundPermissionExplained)
        {
            _backgroundPermissionExplained = true;
            await ShowInfoAsync(
                "Bật theo dõi nền",
                "Để theo dõi vị trí khi ứng dụng chạy nền, hãy chọn quyền vị trí \"Luôn cho phép\".");
        }

        alwaysStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
        if (alwaysStatus == PermissionStatus.Granted)
        {
            return true;
        }

        var shouldOpenSettings = await ShowConfirmAsync(
            "Thiếu quyền vị trí nền",
            "Android cần quyền vị trí nền để theo dõi ổn định. Bạn có muốn mở Cài đặt để cấp quyền ngay không?",
            "Mở cài đặt",
            "Để sau");

        if (shouldOpenSettings)
        {
            AppInfo.Current.ShowSettingsUI();
        }

        return false;
#else
        return true;
#endif
    }

    // Kiểm tra trạng thái quyền background location hiện tại.
    public async Task<bool> HasBackgroundLocationPermissionAsync()
    {
#if ANDROID
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
        {
            return true;
        }

        var alwaysStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        return alwaysStatus == PermissionStatus.Granted;
#else
        return true;
#endif
    }

    // Dừng vòng lặp tracking và tắt foreground service (Android) nếu đang chạy.
    public void StopTracking()
    {
        if (!_isTracking) return;

        try
        {
            _trackingCts?.Cancel();
            _isTracking = false;
            StopForegroundTrackingServiceIfNeeded();
            // Console.WriteLine("Stopped location tracking");
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

    // Vòng lặp tracking: lấy location định kỳ, phát LocationSampled và LocationChanged.
    private async Task RunTrackingLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                var permissionStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                if (permissionStatus != PermissionStatus.Granted)
                {
                    LocationSampled?.Invoke(this, null);
                }
                else
                {
                    var location = await Geolocation.Default.GetLocationAsync(TrackingRequest);
                    if (location != null)
                    {
                        _lastKnownLocation = location;
                    }

                    LocationSampled?.Invoke(this, location);

                    if (location != null && ShouldPublish(location))
                    {
                        _lastPublishedLocation = location;
                        LocationChanged?.Invoke(this, location);
                    }
                }
            }
            catch (Exception)
            {
                LocationSampled?.Invoke(this, null);
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

    // Chỉ publish LocationChanged khi di chuyển đủ xa để giảm nhiễu và tiết kiệm pin.
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

    // Đảm bảo quyền vị trí foreground đã được cấp trước khi tracking.
    private async Task<bool> EnsureForegroundTrackingPermissionAsync()
    {
        await _permissionLock.WaitAsync();
        try
        {
            var whileInUseStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
            if (whileInUseStatus != PermissionStatus.Granted)
            {
                if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
                {
                    await ShowInfoAsync(
                        "Cần quyền vị trí",
                        "Ứng dụng cần quyền truy cập vị trí để phát hiện POI gần bạn.");
                }

                whileInUseStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
            }

            if (whileInUseStatus != PermissionStatus.Granted)
            {
                return false;
            }

#if ANDROID
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                // Notification permission is optional for location data, but requested so foreground
                // notification can be shown reliably on Android 13+.
                var notificationStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
                if (notificationStatus != PermissionStatus.Granted)
                {
                    _ = await Permissions.RequestAsync<Permissions.PostNotifications>();
                }
            }
#endif

            return true;
        }
        finally
        {
            _permissionLock.Release();
        }
    }

    // Hiển thị thông báo 1 nút trên UI thread.
    private static Task ShowInfoAsync(string title, string message)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlertAsync(title, message, "Đóng");
            }
        });
    }

    // Hiển thị confirm dialog trên UI thread và trả về lựa chọn người dùng.
    private static Task<bool> ShowConfirmAsync(
        string title,
        string message,
        string accept,
        string cancel)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page == null)
            {
                return false;
            }

            return await page.DisplayAlertAsync(title, message, accept, cancel);
        });
    }

    // Khởi động foreground tracking service trên Android để giảm rủi ro bị kill nền.
    private static void StartForegroundTrackingServiceIfNeeded()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        var intent = new global::Android.Content.Intent(
            context,
            typeof(global::food_market_narrator.Platforms.Android.TrackingForegroundService));
        intent.SetAction(global::food_market_narrator.Platforms.Android.TrackingForegroundService.ActionStart);

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
#endif
    }

    // Gửi tín hiệu stop cho foreground tracking service trên Android.
    private static void StopForegroundTrackingServiceIfNeeded()
    {
#if ANDROID
        var context = global::Android.App.Application.Context;
        var stopIntent = new global::Android.Content.Intent(
            context,
            typeof(global::food_market_narrator.Platforms.Android.TrackingForegroundService));
        stopIntent.SetAction(global::food_market_narrator.Platforms.Android.TrackingForegroundService.ActionStop);

        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            context.StartForegroundService(stopIntent);
        }
        else
        {
            context.StartService(stopIntent);
        }
#endif
    }
}

