using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

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
    }

    protected override void OnNewIntent(Android.Content.Intent? intent)
    {
        base.OnNewIntent(intent);
        Intent = intent;
    }
}