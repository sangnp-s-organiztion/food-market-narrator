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

    // hàm này được dùng để ghi lại thông tin về việc phát audio, bao gồm nhà hàng, audioId, thời gian bắt đầu và kết thúc, cũng như thời lượng đã phát. Nó sẽ gửi thông tin này lên backend để lưu trữ và phân tích sau này. Hàm cũng xử lý một số trường hợp đặc biệt như session bị mất trên backend và sẽ thử tạo lại session và gửi lại log nếu cần thiết.
    public async Task LogPlaybackAsync(
        string restaurantId,
        int audioId,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        int? playedDurationSeconds = null,
        int? trackDurationSeconds = null,
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

        var duration = playedDurationSeconds.GetValueOrDefault();
        if (duration <= 0)
        {
            duration = (int)Math.Round((normalizedEnd - normalizedStart).TotalSeconds);
        }

        if (trackDurationSeconds.GetValueOrDefault() > 0)
        {
            duration = Math.Min(duration, trackDurationSeconds!.Value);
        }

        duration = Math.Max(0, duration);
        normalizedEnd = normalizedStart.AddSeconds(duration);

        var request = new AudioLogCreateRequest
        {
            SessionId = sessionId,
            RestaurantId = restaurantId.Trim(),
            AudioId = audioId,
            StartTime = normalizedStart,
            EndTime = normalizedEnd,
            Duration = duration
        };

        try
        {
            using var response = await SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return;
            }

            // Only retry for the expected race condition: session missing on backend.
            if (await ShouldRetryForMissingSessionAsync(response, cancellationToken))
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

    // hàm này được dùng để chuẩn hóa thời gian UTC, đảm bảo rằng thời gian được gửi lên backend luôn ở định dạng UTC và có kiểu DateTimeKind.Utc. Nếu thời gian đầu vào là default (chưa được gán), nó sẽ trả về thời gian hiện tại theo UTC.
    private static DateTime NormalizeUtc(DateTime timestamp)
    {
        if (timestamp == default)
        {
            return DateTime.UtcNow;
        }

        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }

    // hàm này được dùng để gửi yêu cầu ghi log phát audio lên backend. Nó sẽ sử dụng HttpClient để gửi một POST request với nội dung là AudioLogCreateRequest được chuyển đổi thành JSON. Hàm này trả về HttpResponseMessage để caller có thể kiểm tra kết quả và xử lý lỗi nếu cần thiết.
    private Task<HttpResponseMessage> SendAsync(AudioLogCreateRequest request, CancellationToken cancellationToken)
    {
        return _httpClient.PostAsJsonAsync(
            AppSettings.AudioLogsEndpoint,
            request,
            cancellationToken);
    }

    // hàm này được dùng để kiểm tra xem có cần thử lại yêu cầu khi session bị mất trên backend hay không. Nó sẽ kiểm tra mã trạng thái của phản hồi và nội dung của phản hồi để xác định xem có phải là lỗi session not found hay không.
    private static async Task<bool> ShouldRetryForMissingSessionAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.StatusCode != HttpStatusCode.NotFound)
        {
            return false;
        }

        try
        {
            var payload = await response.Content.ReadFromJsonAsync<AudioLogErrorResponse>(cancellationToken: cancellationToken);
            var message = (payload?.Message ?? string.Empty).Trim();
            return string.Equals(message, "Session not found", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    // hàm này được dùng để đảm bảo rằng session đã được tạo trên backend trước khi gửi log phát audio. Nếu session chưa tồn tại, nó sẽ gửi một yêu cầu để tạo session mới với thông tin thiết bị và sau đó tiếp tục gửi log. Điều này giúp tránh lỗi khi backend không tìm thấy session tương ứng với log đang gửi.
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

    // hàm này được dùng để lấy hoặc tạo một deviceId duy nhất cho thiết bị hiện tại. DeviceId này sẽ được sử dụng để theo dõi các phiên session của người dùng trên backend. Nếu deviceId đã tồn tại trong Preferences, nó sẽ trả về deviceId đó. Nếu chưa tồn tại, nó sẽ tạo một deviceId mới bằng cách sinh ra một GUID, lưu vào Preferences và trả về deviceId mới đó.
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

public class AudioLogErrorResponse
{
    public string Message { get; set; } = string.Empty;
}
