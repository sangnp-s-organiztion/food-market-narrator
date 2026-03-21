using Microsoft.Maui.Devices;

namespace food_market_narrator.Settings;

public static class AppSettings
{
    // Chi can sua 1 dong nay khi IP may chay API thay doi.
    private const string LocalApiHost = "192.168.1.8";
    private const int HttpPort = 5044;
    private const int HttpsPort = 7041;

    private static string BuildHttpBaseUrl(string host) => $"http://{host}:{HttpPort}/";
    private static string BuildHttpsBaseUrl(string host) => $"https://{host}:{HttpsPort}/";

#if ANDROID
    private static string ActiveApiHost =>
        DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : LocalApiHost;

    public static string ApiBaseUrl
    {
        get { return BuildHttpBaseUrl(ActiveApiHost); }
    }

    public static string[] ApiFallbackBaseUrls
    {
        get { return new[] { BuildHttpBaseUrl(ActiveApiHost), BuildHttpsBaseUrl(ActiveApiHost) }; }
    }
#else
    public static string ApiBaseUrl => BuildHttpBaseUrl(LocalApiHost);
    public static readonly string[] ApiFallbackBaseUrls =
    {
        BuildHttpBaseUrl(LocalApiHost),
        BuildHttpsBaseUrl(LocalApiHost)
    };
#endif

    public const string RestaurantEndpoint = "restaurant";
    public const string LanguageEndpoint = "language";

    public const double MapHighlightDistanceMeters = 20;
    public const double TriggerDistanceMeters = 30;
    public const double PoiEnterRadiusMeters = 30;
    public const double PoiExitRadiusMeters = 40;
}