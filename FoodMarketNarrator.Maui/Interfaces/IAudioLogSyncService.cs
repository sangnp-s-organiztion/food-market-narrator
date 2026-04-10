namespace food_market_narrator.Services;

public interface IAudioLogSyncService
{
    Task LogPlaybackAsync(
        string restaurantId,
        int audioId,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
    int? playedDurationSeconds = null,
    int? trackDurationSeconds = null,
        CancellationToken cancellationToken = default);
}
