namespace food_market_narrator.Services;

public interface IAudioService
{
    bool IsPlaying { get; }
    Task PlaySound(string language, string fileName);
    void StopSound();
}
