namespace food_market_narrator.Services;

public interface IAudioService
{
    bool IsPlaying { get; }
    TimeSpan Duration { get; }
    TimeSpan CurrentPosition { get; }
    event EventHandler? PlaybackEnded;
    Task PlaySound(string language, string fileName);
    void StopSound();
}
