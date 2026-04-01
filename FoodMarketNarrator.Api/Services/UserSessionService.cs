using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Repositories;

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

        await _userSessionRepository.UpsertStartAsync(new UserSessionStartRecord
        {
            SessionId = sessionId,
            DeviceId = deviceId,
            DeviceInfo = string.IsNullOrWhiteSpace(deviceInfo) ? "unknown" : deviceInfo
        });
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
}
