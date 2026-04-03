namespace food_market_narrator.Services;

public interface IAudioLogSyncService
{
    Task LogPlaybackAsync(
        string restaurantId,
        int audioId,
        DateTime startTimeUtc,
        DateTime endTimeUtc,
        CancellationToken cancellationToken = default);
}
