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
    bool IsCurrentTrack(string language, string fileName);
    void Pause();
    void Resume();
    void StopSound();
}
