using food_market_narrator.Settings;
using System.Net.Http.Json;
using System.Net;
using Microsoft.Maui.Devices;
using Microsoft.Maui.Storage;

namespace food_market_narrator.Services;

public class AudioLogSyncService : IAudioLogSyncService
{
    private const string DeviceIdPreferenceKey = "tracking_device_id";

    private readonly HttpClient _httpClient;
    private readonly ILocationLogSyncService _locationLogSyncService;

    public AudioLogSyncService(HttpClient httpClient, ILocationLogSyncService locationLogSyncService)
    {
        _httpClient = httpClient;
        _locationLogSyncService = locationLogSyncService;
    }

    public async Task LogPlaybackAsync(
        string restaurantId,
        int audioId,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(restaurantId) || audioId <= 0)
        {
            return;
        }

        var sessionId = (_locationLogSyncService.CurrentSessionId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return;
        }

        // Audio có thể được phát từ POI detail khi tracking chưa flush location,
        // nên cần đảm bảo session đã tồn tại trước khi gửi audio log.
        await EnsureSessionStartedAsync(sessionId, cancellationToken);

        var normalizedStart = NormalizeUtc(startTimeUtc);
        var normalizedEnd = NormalizeUtc(endTimeUtc);
        if (normalizedEnd < normalizedStart)
        {
            normalizedEnd = normalizedStart;
        }

        var duration = (int)Math.Round((normalizedEnd - normalizedStart).TotalSeconds);

        var request = new AudioLogCreateRequest
        {
            SessionId = sessionId,
            RestaurantId = restaurantId.Trim(),
            AudioId = audioId,
            StartTime = normalizedStart,
            EndTime = normalizedEnd,
            Duration = Math.Max(0, duration)
        };

        try
        {
            using var response = await SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            // Session could be missing on backend when audio log arrives before session start sync.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                await EnsureSessionStartedAsync(sessionId, cancellationToken);
                await _locationLogSyncService.FlushNowAsync();
                using var retryResponse = await SendAsync(request, cancellationToken);
                if (retryResponse.IsSuccessStatusCode)
                {
                    return;
                }

                Console.WriteLine($"Sync audio log: retry failed with status {(int)retryResponse.StatusCode}");
                return;
            }

            Console.WriteLine($"Sync audio log: failed with status {(int)response.StatusCode}");
        }
        catch (Exception)
        {
            Console.WriteLine("Sync audio log: exception while sending audio log");
        }
    }

    private static DateTime NormalizeUtc(DateTime timestamp)
    {
        if (timestamp == default)
        {
            return DateTime.UtcNow;
        }

        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    private Task<HttpResponseMessage> SendAsync(AudioLogCreateRequest request, CancellationToken cancellationToken)
    {
        return _httpClient.PostAsJsonAsync(
            AppSettings.AudioLogsEndpoint,
            request,
            cancellationToken);
    }

    private async Task EnsureSessionStartedAsync(string sessionId, CancellationToken cancellationToken)
    {
        var request = new AudioUserSessionStartRequest
        {
            SessionId = sessionId,
            DeviceId = GetOrCreateDeviceId(),
            DeviceInfo = $"{DeviceInfo.Manufacturer} {DeviceInfo.Model}, {DeviceInfo.Platform} {DeviceInfo.VersionString}"
        };

        try
        {
            using var response = await _httpClient.PostAsJsonAsync(
                AppSettings.UserSessionsStartEndpoint,
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Sync session start (audio): failed with status {(int)response.StatusCode}");
            }
        }
        catch (Exception)
        {
            Console.WriteLine("Sync session start (audio): exception while creating session");
        }
    }

    private static string GetOrCreateDeviceId()
    {
        var existingDeviceId = Preferences.Get(DeviceIdPreferenceKey, string.Empty);
        if (!string.IsNullOrWhiteSpace(existingDeviceId))
        {
            return existingDeviceId;
        }

        var generated = Guid.NewGuid().ToString("N");
        Preferences.Set(DeviceIdPreferenceKey, generated);
        return generated;
    }
}

public class AudioLogCreateRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string RestaurantId { get; set; } = string.Empty;
    public int AudioId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public int Duration { get; set; }
}

public class AudioUserSessionStartRequest
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
}
