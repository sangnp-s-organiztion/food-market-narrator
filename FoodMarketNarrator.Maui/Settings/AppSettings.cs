using Microsoft.Maui.Devices;

namespace food_market_narrator.Settings;

public static class AppSettings
{
    // Chi can sua 1 dong nay khi IP may chay API thay doi.
    private const string LocalApiHost = "192.168.1.7";
    // Neu deploy cloud (Render), gan full base URL https tai day.
    // Vi du: https://food-market-narrator-api.onrender.com/
    private const string CloudApiBaseUrl = "https://food-market-narrator-api.onrender.com/";
    private const int HttpPort = 5044;
    private const int HttpsPort = 7041;

    private static string BuildHttpBaseUrl(string host) => $"http://{host}:{HttpPort}/";
    private static string BuildHttpsBaseUrl(string host) => $"https://{host}:{HttpsPort}/";
    private static bool HasCloudApiBaseUrl => !string.IsNullOrWhiteSpace(CloudApiBaseUrl);

    private static string NormalizeBaseUrl(string baseUrl)
    {
        var trimmed = (baseUrl ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        return trimmed.EndsWith("/", StringComparison.Ordinal) ? trimmed : $"{trimmed}/";
    }

#if ANDROID
    private static string ActiveApiHost =>
        DeviceInfo.DeviceType == DeviceType.Virtual ? "10.0.2.2" : LocalApiHost;

    private static bool UseCloudApiOnAndroid =>
        DeviceInfo.DeviceType != DeviceType.Virtual && HasCloudApiBaseUrl;

    public static string ApiBaseUrl
    {
        get
        {
            if (UseCloudApiOnAndroid)
            {
                return NormalizeBaseUrl(CloudApiBaseUrl);
            }

            return BuildHttpBaseUrl(ActiveApiHost);
        }
    }

    public static string[] ApiFallbackBaseUrls
    {
        get
        {
            if (UseCloudApiOnAndroid)
            {
                return new[] { NormalizeBaseUrl(CloudApiBaseUrl) };
            }

            return new[] { BuildHttpBaseUrl(ActiveApiHost), BuildHttpsBaseUrl(ActiveApiHost) };
        }
    }
#else
    public static string ApiBaseUrl =>
        HasCloudApiBaseUrl ? NormalizeBaseUrl(CloudApiBaseUrl) : BuildHttpBaseUrl(LocalApiHost);

    public static readonly string[] ApiFallbackBaseUrls = HasCloudApiBaseUrl
        ? [NormalizeBaseUrl(CloudApiBaseUrl)]
        : [
            BuildHttpBaseUrl(LocalApiHost),
            BuildHttpsBaseUrl(LocalApiHost)
        ];
#endif

    public const string RestaurantEndpoint = "restaurant";
    public const string TourEndpoint = "tour";
    public const string LanguageEndpoint = "language";
    public const string PublicTranslationsEndpoint = "public/translations";
    public const string UserSessionsStartEndpoint = "api/user-sessions/start";
    public const string LocationLogsBatchEndpoint = "api/location-logs/batch";
    public const string AudioLogsEndpoint = "api/audio-logs";
    public const string MapTileCacheParentFolderName = "map_cache";
    public const string MapTileCacheFolderName = "osm_tiles";

    public static string MapTileCacheDirectory =>
        Path.Combine(FileSystem.AppDataDirectory, MapTileCacheParentFolderName, MapTileCacheFolderName);

    public const double MapHighlightDistanceMeters = 20;
    public const double TriggerDistanceMeters = 30;
    public const double PoiEnterRadiusMeters = 30;
    public const double PoiExitRadiusMeters = 40;

    // Performance tuning for startup/warm-up on mobile devices.

    // đợi 1.2s rồi mới gọi start tracking gps
    public const int StartupTrackingDelayMs = 1200;

    // trì hoãn warm-up dữ liệu sau khi app đã vào UI để giảm giật lúc startup.
    public const int StartupWarmupDelayMs = 3500;

    // trì hoãn 2 giây trước khi bắt đầu warm-up offline (ảnh/dishes).
    public const int OfflineWarmupInitialDelayMs = 2000;

    // phase A chạy trước (nhóm POI ưu tiên), phase B chạy sau 10 giây.
    public const int OfflineWarmupPhaseBDelayMs = 10000;

    // chỉ cho 1 job tải ảnh chạy đồng thời.
    public const int OfflineWarmupImageConcurrency = 1;

    //  tắt log ảnh quá chi tiết mặc định (enqueue/url-candidates/download-flow-success hàng loạt).
    public static readonly bool EnableVerboseImageWarmupLogs = false;
}