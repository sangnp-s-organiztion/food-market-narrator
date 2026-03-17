using Android.App;
using Android.Content;
using Android.OS;
using AndroidX.Core.App;
using System;

namespace food_market_narrator.Platforms.Android;

[Service(Exported = false, ForegroundServiceType = global::Android.Content.PM.ForegroundService.TypeLocation)]
public class TrackingForegroundService : Service
{
    public const string ChannelId = "location_tracking_channel";
    public const int NotificationId = 4201;
    public const string ActionStart = "food_market_narrator.action.START_TRACKING";
    public const string ActionStop = "food_market_narrator.action.STOP_TRACKING";

    public override IBinder? OnBind(Intent? intent)
    {
        return null;
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action;
        if (action == ActionStop)
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
            {
                StopForeground(StopForegroundFlags.Remove);
            }
            else
            {
#pragma warning disable CS0618
                StopForeground(true);
#pragma warning restore CS0618
            }

            StopSelf();
            return StartCommandResult.NotSticky;
        }

        CreateNotificationChannel();
        StartForeground(NotificationId, BuildNotification());

        return StartCommandResult.Sticky;
    }

    private Notification BuildNotification()
    {
        var stopIntent = new Intent(this, typeof(TrackingForegroundService));
        stopIntent.SetAction(ActionStop);

        var pendingIntentFlags = PendingIntentFlags.UpdateCurrent;
        if (OperatingSystem.IsAndroidVersionAtLeast(23))
        {
            pendingIntentFlags |= PendingIntentFlags.Immutable;
        }

        var stopPendingIntent = PendingIntent.GetService(
            this,
            0,
            stopIntent,
            pendingIntentFlags);

        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetContentTitle("Food Market Narrator");
        builder.SetContentText("Dang theo doi vi tri nen");
        builder.SetSmallIcon(Resource.Mipmap.appicon);
        builder.SetOngoing(true);
        builder.SetPriority((int)NotificationPriority.Low);

        if (stopPendingIntent != null)
        {
            builder.AddAction(0, "Dung", stopPendingIntent);
        }

        var notification = builder.Build();
        return notification ?? throw new InvalidOperationException("Unable to build foreground notification.");
    }

    private void CreateNotificationChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            return;
        }

        var managerObject = GetSystemService(NotificationService);
        if (managerObject is not NotificationManager manager)
        {
            return;
        }

        var channel = new NotificationChannel(
            ChannelId,
            "Location Tracking",
            NotificationImportance.Low)
        {
            Description = "Foreground service for background location tracking"
        };

        manager.CreateNotificationChannel(channel);
    }
}
