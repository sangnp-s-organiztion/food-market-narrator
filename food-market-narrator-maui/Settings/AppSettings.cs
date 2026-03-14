namespace food_market_narrator.Settings;

public static class AppSettings
{
#if ANDROID
    public const string ApiBaseUrl = "http://10.0.2.2:5044/";
    public static readonly string[] ApiFallbackBaseUrls =
    {
        "http://10.0.2.2:5044/",
        "https://10.0.2.2:7041/"
    };
#else
    public const string ApiBaseUrl = "http://localhost:5044/";
    public static readonly string[] ApiFallbackBaseUrls =
    {
        "http://localhost:5044/",
        "https://localhost:7041/"
    };
#endif

    public const string RestaurantEndpoint = "restaurant";
    public const string LanguageEndpoint = "language";
    public const double MapHighlightDistanceMeters = 20;
    public const double TriggerDistanceMeters = 30;
    public const double PoiEnterRadiusMeters = 30;
    public const double PoiExitRadiusMeters = 40;
}