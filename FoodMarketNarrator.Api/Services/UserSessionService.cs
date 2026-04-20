using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Repositories;
using MongoDB.Bson;

namespace food_market_narrator_api.Services;

public class UserSessionService
{
    private readonly UserSessionRepository _userSessionRepository;

    public UserSessionService(UserSessionRepository userSessionRepository)
    {
        _userSessionRepository = userSessionRepository;
    }

    public async Task StartSessionAsync(UserSessionStartRequest request)
    {
        var sessionId = request.SessionId.Trim();
        var deviceId = request.DeviceId.Trim();
        var deviceInfo = (request.DeviceInfo ?? string.Empty).Trim();
        var qrAccessExpiresAtUtc = request.QrAccessExpiresAtUtc.HasValue
            ? DateTime.SpecifyKind(request.QrAccessExpiresAtUtc.Value, DateTimeKind.Utc)
            : (DateTime?)null;

        await _userSessionRepository.UpsertStartAsync(new UserSessionStartRecord
        {
            SessionId = sessionId,
            DeviceId = deviceId,
            DeviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? "unknown" : deviceInfo,
            QrAccessExpiresAtUtc = qrAccessExpiresAtUtc
        });
    }

    public async Task<int> CountVisitorsAsync()
    {
        var count = await _userSessionRepository.CountVisitorsAsync();
        return count > int.MaxValue ? int.MaxValue : (int)count;
    }

    public Task<IReadOnlyList<VisitorSessionRecord>> GetVisitorsAsync(int limit = 200)
    {
        return _userSessionRepository.GetVisitorsAsync(limit);
    }

    public async Task TouchSessionActivityAsync(IReadOnlyCollection<string> sessionIds, DateTime lastSeenAtUtc)
    {
        var normalizedSessionIds = sessionIds
            .Where(sessionId => !string.IsNullOrWhiteSpace(sessionId))
            .Select(sessionId => sessionId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedSessionIds.Count == 0)
        {
            return;
        }

        var normalizedLastSeen = DateTime.SpecifyKind(lastSeenAtUtc, DateTimeKind.Utc);
        await _userSessionRepository.TouchSessionsAsync(normalizedSessionIds, normalizedLastSeen);
    }

    public Task<ObjectId?> FindObjectIdBySessionIdAsync(string sessionId)
    {
        return _userSessionRepository.FindObjectIdBySessionIdAsync(sessionId);
    }

    public async Task<UserSessionQrAccessStatus> GetQrAccessStatusAsync(string sessionId)
    {
        var record = await _userSessionRepository.GetQrAccessBySessionIdAsync(sessionId);
        if (record == null)
        {
            return new UserSessionQrAccessStatus
            {
                Exists = false,
                Allowed = false,
                Reason = "session_not_found"
            };
        }

        if (!record.QrAccessExpiresAtUtc.HasValue)
        {
            return new UserSessionQrAccessStatus
            {
                Exists = true,
                Allowed = true,
                Reason = "unrestricted"
            };
        }

        var expiresAtUtc = DateTime.SpecifyKind(record.QrAccessExpiresAtUtc.Value, DateTimeKind.Utc);
        var isAllowed = DateTime.UtcNow <= expiresAtUtc;

        return new UserSessionQrAccessStatus
        {
            Exists = true,
            Allowed = isAllowed,
            ExpiresAtUtc = expiresAtUtc,
            Reason = isAllowed ? "active" : "expired"
        };
    }
}

public class UserSessionQrAccessStatus
{
    public bool Exists { get; set; }
    public bool Allowed { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public string Reason { get; set; } = string.Empty;
}
