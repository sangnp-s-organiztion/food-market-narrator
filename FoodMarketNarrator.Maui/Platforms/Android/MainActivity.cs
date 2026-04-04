using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.OS;
using Android.Views;
using food_market_narrator.Services;

namespace food_market_narrator;

[Activity(Theme = "@style/Maui.SplashTheme", 
          MainLauncher = true, 
          LaunchMode = LaunchMode.SingleTop, 
          ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | 
                                  ConfigChanges.UiMode | ConfigChanges.ScreenLayout | 
                                  ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
[IntentFilter(
    new[] { Android.Content.Intent.ActionView },
    Categories = new[] { Android.Content.Intent.CategoryDefault, Android.Content.Intent.CategoryBrowsable },
    DataScheme = "foodmarketnarrator",
    DataHost = "open"
)]
public class MainActivity : MauiAppCompatActivity
{
    protected override void OnCreate(Bundle? savedInstanceState)
    {
        // ⬇ Switch sang main theme TRƯỚC khi base.OnCreate inflate layout
        SetTheme(Resource.Style.Maui_MainTheme_NoActionBar);
        base.OnCreate(savedInstanceState);
        HandleDeepLinkIntent(Intent);

        // Đồng bộ thanh status bar theo tông sáng của app, tránh dải tím mặc định.
        if (Window != null)
        {
            Window.SetStatusBarColor(Android.Graphics.Color.White);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                var flags = (StatusBarVisibility)SystemUiFlags.LightStatusBar;
                Window.DecorView.SystemUiVisibility = flags;
            }
        }
    }

    protected override void OnNewIntent(Android.Content.Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
        HandleDeepLinkIntent(intent);
    }

    private static void HandleDeepLinkIntent(Android.Content.Intent? intent)
    {
        var dataString = intent?.DataString;
        if (string.IsNullOrWhiteSpace(dataString))
        {
            return;
        }

        AppLinkDispatcher.Dispatch(dataString);
    }
}