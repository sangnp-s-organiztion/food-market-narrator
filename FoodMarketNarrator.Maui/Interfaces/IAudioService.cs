namespace food_market_narrator.Services;

public interface IAudioService
{
    bool IsPlaying { get; }
    bool IsPaused { get; }
    string? CurrentTrackKey { get; }
    TimeSpan Duration { get; }
    TimeSpan CurrentPosition { get; }
    event EventHandler? PlaybackEnded;
    Task PlaySound(string language, string fileName);
    Task PlaySound(int audioId);
    bool IsCurrentTrack(string language, string fileName);
    bool IsCurrentTrack(int audioId);
    bool HasLocalAudio(string language, string fileName);
    bool HasLocalAudio(int audioId);
    Task<bool> PrefetchAudioAsync(string language, string fileName);
    Task<bool> PrefetchAudioAsync(int audioId);
    Task<long> GetCachedAudioSizeBytesAsync();
    Task ClearAudioCacheAsync();
    void Pause();
    void Resume();
    void StopSound();
}
