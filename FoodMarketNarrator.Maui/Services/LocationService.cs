using Microsoft.Maui.Devices.Sensors;
using System.Linq;

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
    private bool _backgroundPermissionExplained;

    public event EventHandler<Location>? LocationChanged;
    public event EventHandler<Location?>? LocationSampled;

    // Lay vi tri hien tai cua nguoi dung.
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var granted = await EnsureForegroundTrackingPermissionAsync();
            if (!granted)
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

        var granted = await EnsureForegroundTrackingPermissionAsync();

        try
        {
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
    }

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
                "Bat tracking nen",
                "De theo doi vi tri khi app chay nen, hay chon phep vi tri \"Always allow\".");
        }

        alwaysStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
        if (alwaysStatus == PermissionStatus.Granted)
        {
            return true;
        }

        var shouldOpenSettings = await ShowConfirmAsync(
            "Thieu quyen vi tri nen",
            "Android can quyen vi tri nen de tracking on dinh. Ban co muon mo Settings de cap quyen ngay khong?",
            "Mo Settings",
            "De sau");

        if (shouldOpenSettings)
        {
            AppInfo.Current.ShowSettingsUI();
        }

        return false;
#else
        return true;
#endif
    }

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

    private async Task<bool> EnsureForegroundTrackingPermissionAsync()
    {
        var whileInUseStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (whileInUseStatus != PermissionStatus.Granted)
        {
            if (Permissions.ShouldShowRationale<Permissions.LocationWhenInUse>())
            {
                await ShowInfoAsync(
                    "Can quyen vi tri",
                    "Ung dung can quyen truy cap vi tri de phat hien POI gan ban.");
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

    private static Task ShowInfoAsync(string title, string message)
    {
        return MainThread.InvokeOnMainThreadAsync(async () =>
        {
            var page = Application.Current?.Windows.FirstOrDefault()?.Page;
            if (page != null)
            {
                await page.DisplayAlertAsync(title, message, "OK");
            }
        });
    }

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

