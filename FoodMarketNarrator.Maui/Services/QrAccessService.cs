using food_market_narrator.Settings;
using System.Globalization;
using System.Net.Http.Json;

namespace food_market_narrator.Services;

public class QrAccessService : IQrAccessService
{
    private static readonly TimeSpan ServerCheckThrottle = TimeSpan.FromSeconds(10);

    private readonly HttpClient _httpClient;
    private readonly object _stateLock = new();

    private bool _isQrTimeRestricted;
    private DateTime? _qrAccessExpiresAtUtc;
    private DateTime _lastServerCheckUtc = DateTime.MinValue;
    private bool _lastServerAllowed = true;
    private string _lastCheckedSessionId = string.Empty;
    private string _lastBlockReason = string.Empty;

    public QrAccessService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public bool IsQrTimeRestricted
    {
        get
        {
            lock (_stateLock)
            {
                return _isQrTimeRestricted;
            }
        }
    }

    public DateTime? QrAccessExpiresAtUtc
    {
        get
        {
            lock (_stateLock)
            {
                return _qrAccessExpiresAtUtc;
            }
        }
    }

    public string LastBlockReason
    {
        get
        {
            lock (_stateLock)
            {
                return _lastBlockReason;
            }
        }
    }

    public void ApplyDeepLink(string deepLinkUrl)
    {
        if (!TryParseDeepLink(deepLinkUrl, out var uri))
        {
            return;
        }

        var hasExpiry = TryExtractExpiryFromDeepLink(uri, out var expiresAtUtc);

        lock (_stateLock)
        {
            if (!hasExpiry)
            {
                // Deep link khong co time-window thi app hoat dong binh thuong.
                _isQrTimeRestricted = false;
                _qrAccessExpiresAtUtc = null;
                _lastBlockReason = string.Empty;
                return;
            }

            _isQrTimeRestricted = true;
            _qrAccessExpiresAtUtc = expiresAtUtc;
            _lastServerCheckUtc = DateTime.MinValue;
            _lastServerAllowed = true;
            _lastCheckedSessionId = string.Empty;
            _lastBlockReason = string.Empty;
        }
    }

    public async Task<bool> CanContinueNarrationAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        DateTime? localExpiry;
        bool isRestricted;
        lock (_stateLock)
        {
            isRestricted = _isQrTimeRestricted;
            localExpiry = _qrAccessExpiresAtUtc;
        }

        if (!isRestricted)
        {
            return true;
        }

        if (localExpiry.HasValue && DateTime.UtcNow > localExpiry.Value)
        {
            lock (_stateLock)
            {
                _lastServerAllowed = false;
                _lastBlockReason = "expired";
            }

            return false;
        }

        var normalizedSessionId = (sessionId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedSessionId))
        {
            return true;
        }

        lock (_stateLock)
        {
            var sinceLastCheck = DateTime.UtcNow - _lastServerCheckUtc;
            if (string.Equals(_lastCheckedSessionId, normalizedSessionId, StringComparison.Ordinal)
                && sinceLastCheck < ServerCheckThrottle)
            {
                return _lastServerAllowed;
            }
        }

        try
        {
            var endpoint = string.Format(
                CultureInfo.InvariantCulture,
                AppSettings.UserSessionQrAccessEndpointFormat,
                Uri.EscapeDataString(normalizedSessionId));

            var response = await _httpClient.GetFromJsonAsync<QrAccessStatusResponse>(endpoint, cancellationToken);
            if (response == null)
            {
                return true;
            }

            lock (_stateLock)
            {
                _lastServerCheckUtc = DateTime.UtcNow;
                _lastCheckedSessionId = normalizedSessionId;
                _lastServerAllowed = response.Allowed;
                _lastBlockReason = response.Reason ?? string.Empty;

                if (response.ExpiresAtUtc.HasValue)
                {
                    _qrAccessExpiresAtUtc = _qrAccessExpiresAtUtc.HasValue
                        ? MinUtc(_qrAccessExpiresAtUtc.Value, response.ExpiresAtUtc.Value)
                        : response.ExpiresAtUtc.Value;
                }
            }

            return response.Allowed;
        }
        catch
        {
            // Loi mang khong duoc lam ngat narration ngay, fallback theo local expiry.
            return true;
        }
    }

    private static bool TryParseDeepLink(string deepLinkUrl, out Uri uri)
    {
        uri = default!;
        if (!Uri.TryCreate(deepLinkUrl, UriKind.Absolute, out var parsedUri))
        {
            return false;
        }

        if (!string.Equals(parsedUri.Scheme, "foodmarketnarrator", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (!string.Equals(parsedUri.Host, "open", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        uri = parsedUri;
        return true;
    }

    private static bool TryExtractExpiryFromDeepLink(Uri uri, out DateTime expiresAtUtc)
    {
        expiresAtUtc = default;
        var queryMap = ParseQuery(uri.Query);

        if (TryGetDateTimeQuery(queryMap, out expiresAtUtc, "expiresAtUtc", "expiresAt", "until"))
        {
            return true;
        }

        if (TryGetIntQuery(queryMap, out var durationMinutes, "durationMinutes", "durationMins", "ttlMinutes")
            && durationMinutes > 0)
        {
            expiresAtUtc = DateTime.UtcNow.AddMinutes(durationMinutes);
            return true;
        }

        if (TryGetIntQuery(queryMap, out var durationSeconds, "durationSeconds", "ttlSeconds")
            && durationSeconds > 0)
        {
            expiresAtUtc = DateTime.UtcNow.AddSeconds(durationSeconds);
            return true;
        }

        return false;
    }

    private static DateTime MinUtc(DateTime lhs, DateTime rhs)
    {
        return lhs <= rhs ? lhs : rhs;
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(query))
        {
            return result;
        }

        var trimmed = query.TrimStart('?');
        var pairs = trimmed.Split('&', StringSplitOptions.RemoveEmptyEntries);

        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 0 || string.IsNullOrWhiteSpace(parts[0]))
            {
                continue;
            }

            var key = Uri.UnescapeDataString(parts[0]);
            var value = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : string.Empty;
            result[key] = value;
        }

        return result;
    }

    private static bool TryGetDateTimeQuery(
        IReadOnlyDictionary<string, string> queryMap,
        out DateTime valueUtc,
        params string[] keys)
    {
        valueUtc = default;

        foreach (var key in keys)
        {
            if (!queryMap.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (!DateTime.TryParse(
                    raw,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out var parsed))
            {
                continue;
            }

            valueUtc = DateTime.SpecifyKind(parsed, DateTimeKind.Utc);
            return true;
        }

        return false;
    }

    private static bool TryGetIntQuery(IReadOnlyDictionary<string, string> queryMap, out int value, params string[] keys)
    {
        value = 0;
        foreach (var key in keys)
        {
            if (!queryMap.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
            {
                continue;
            }

            if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
            {
                value = parsed;
                return true;
            }
        }

        return false;
    }

    private sealed class QrAccessStatusResponse
    {
        public bool Allowed { get; set; }
        public DateTime? ExpiresAtUtc { get; set; }
        public string? Reason { get; set; }
    }
}