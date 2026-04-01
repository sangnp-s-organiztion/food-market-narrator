using Microsoft.Maui.Devices.Sensors;
using System.Linq;

namespace food_market_narrator.Services;

public class LocationService : ILocationService
{
    private const string BackgroundTrackingModeKey = "background_tracking_mode_enabled";
    private bool _isTracking = false;
    private CancellationTokenSource? _trackingCts;
    private Task? _trackingTask;
    private Location? _lastPublishedLocation;
    private readonly SemaphoreSlim _permissionFlowLock = new(1, 1);
    private bool _hasPermissionFlowCompleted;
    private bool _cachedTrackingPermissionGranted;

    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const double MinPublishDistanceMeters = 6;
    private static readonly GeolocationRequest TrackingRequest =
        new(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));

    public event EventHandler<Location>? LocationChanged;

    public bool IsBackgroundTrackingModeEnabled => Preferences.Get(BackgroundTrackingModeKey, false);

    // Lay vi tri hien tai cua nguoi dung.
    public async Task<Location?> GetCurrentLocationAsync()
    {
        try
        {
            var granted = await EnsureTrackingPermissionFlowAsync();
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

        var granted = await EnsureTrackingPermissionFlowAsync();
        if (!granted)
        {
            // Console.WriteLine("Location permission not granted");
            return;
        }

        try
        {
            if (IsBackgroundTrackingModeEnabled)
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

    public async Task<bool> SetBackgroundTrackingModeAsync(bool enabled)
    {
#if ANDROID
        if (!enabled)
        {
            Preferences.Set(BackgroundTrackingModeKey, false);

            if (_isTracking)
            {
                StopForegroundTrackingServiceIfNeeded();
            }

            return true;
        }

        var hasForegroundPermission = await EnsureTrackingPermissionFlowAsync();
        if (!hasForegroundPermission)
        {
            Preferences.Set(BackgroundTrackingModeKey, false);
            return false;
        }

        var alwaysStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (alwaysStatus != PermissionStatus.Granted)
        {
            if (Permissions.ShouldShowRationale<Permissions.LocationAlways>())
            {
                await ShowInfoAsync(
                    "Can quyen vi tri nen",
                    "Bat che do theo doi nen can quyen 'Allow all the time'.");
            }

            alwaysStatus = await Permissions.RequestAsync<Permissions.LocationAlways>();
        }

        var granted = alwaysStatus == PermissionStatus.Granted;
        Preferences.Set(BackgroundTrackingModeKey, granted);

        if (granted && _isTracking)
        {
            StartForegroundTrackingServiceIfNeeded();
        }

        return granted;
#else
        Preferences.Set(BackgroundTrackingModeKey, enabled);
        return true;
#endif
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

    private async Task<bool> EnsureTrackingPermissionFlowAsync()
    {
        if (_hasPermissionFlowCompleted)
        {
            if (_cachedTrackingPermissionGranted)
            {
                return true;
            }

            // If user granted permission in Settings after a previous denial,
            // detect it without showing another system prompt.
            var grantedNow = await HasTrackingPermissionWithoutPromptAsync();
            if (grantedNow)
            {
                _cachedTrackingPermissionGranted = true;
            }

            return _cachedTrackingPermissionGranted;
        }

        await _permissionFlowLock.WaitAsync();
        try
        {
            if (_hasPermissionFlowCompleted)
            {
                return _cachedTrackingPermissionGranted;
            }

            _cachedTrackingPermissionGranted = await RequestTrackingPermissionInteractiveAsync();
            _hasPermissionFlowCompleted = true;
            return _cachedTrackingPermissionGranted;
        }
        finally
        {
            _permissionFlowLock.Release();
        }
    }

    private static async Task<bool> RequestTrackingPermissionInteractiveAsync()
    {
#if ANDROID
        // Request foreground permission first to avoid Android 11+ extra
        // permission-management screen shown during background escalation.
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
#else
        // Non-Android: request WhenInUse by default.
        var whileInUseStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        if (whileInUseStatus != PermissionStatus.Granted)
        {
            whileInUseStatus = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        }

        if (whileInUseStatus != PermissionStatus.Granted)
        {
            return false;
        }
#endif

#if ANDROID
        // Notification permission: optional but requested so foreground
        // notification is shown reliably on Android 13+.
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var notificationStatus = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
            if (notificationStatus != PermissionStatus.Granted)
            {
                _ = await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
        }
#endif

        return true;
    }

    private static async Task<bool> HasTrackingPermissionWithoutPromptAsync()
    {
#if ANDROID
        var alwaysStatus = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        if (alwaysStatus == PermissionStatus.Granted)
        {
            return true;
        }

        var whileInUseStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        return whileInUseStatus == PermissionStatus.Granted;
#else
        var whileInUseStatus = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        return whileInUseStatus == PermissionStatus.Granted;
#endif
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

