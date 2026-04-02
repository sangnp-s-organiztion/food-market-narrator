using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Repositories;
using MongoDB.Bson;

namespace food_market_narrator_api.Services;

public class AudioLogService
{
    private readonly AudioLogRepository _audioLogRepository;
    private readonly UserSessionService _userSessionService;

    public AudioLogService(AudioLogRepository audioLogRepository, UserSessionService userSessionService)
    {
        _audioLogRepository = audioLogRepository;
        _userSessionService = userSessionService;
    }

    public async Task<bool> WriteAsync(AudioLogCreateRequest request)
    {
        var sessionObjectId = await ResolveSessionObjectIdAsync(request.SessionId);
        if (!sessionObjectId.HasValue)
        {
            return false;
        }

        var startTimeUtc = NormalizeUtc(request.StartTime);
        var endTimeUtc = NormalizeUtc(request.EndTime);
        if (endTimeUtc < startTimeUtc)
        {
            endTimeUtc = startTimeUtc;
        }

        var duration = request.Duration;
        if (duration <= 0)
        {
            duration = (int)Math.Round((endTimeUtc - startTimeUtc).TotalSeconds);
        }

        await _audioLogRepository.InsertAsync(new AudioLogRecord
        {
            SessionObjectId = sessionObjectId.Value,
            RestaurantId = request.RestaurantId.Trim(),
            AudioId = request.AudioId,
            StartTimeUtc = startTimeUtc,
            EndTimeUtc = endTimeUtc,
            DurationSeconds = Math.Max(0, duration)
        });

        return true;
    }

    private async Task<ObjectId?> ResolveSessionObjectIdAsync(string sessionId)
    {
        var normalizedSessionId = (sessionId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedSessionId))
        {
            return null;
        }

        var existingSessionObjectId = await _userSessionService.FindObjectIdBySessionIdAsync(normalizedSessionId);
        if (existingSessionObjectId.HasValue)
        {
            return existingSessionObjectId;
        }

        if (ObjectId.TryParse(normalizedSessionId, out var parsedObjectId))
        {
            return parsedObjectId;
        }

        return null;
    }

    private static DateTime NormalizeUtc(DateTime timestamp)
    {
        if (timestamp == default)
        {
            return DateTime.UtcNow;
        }

        return DateTime.SpecifyKind(timestamp, DateTimeKind.Utc);
    }
}
