using food_market_narrator.Models;

namespace food_market_narrator.Services;

public interface IAudioService
{
    bool IsPlaying { get; }
    bool IsPaused { get; }
    string? CurrentTrackKey { get; }
    TimeSpan Duration { get; }
    TimeSpan CurrentPosition { get; }
    event EventHandler? PlaybackEnded;
    event EventHandler<long>? CacheSizeChanged;
    Task PlaySound(string language, string fileName);
    Task PreloadAllActiveAudiosAsync(IEnumerable<POI> pois, CancellationToken cancellationToken = default);
    bool IsCurrentTrack(string language, string fileName);
    Task<long> GetCachedAudioSizeBytesAsync();
    Task ClearAudioCacheAsync();
    void Pause();
    void Resume();
    void StopSound();
}
