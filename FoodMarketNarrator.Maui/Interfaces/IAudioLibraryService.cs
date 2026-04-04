namespace food_market_narrator.Services;

public interface IAudioLibraryService
{
    Task InitializeOnStartupAsync();
    bool ConsumeStartupOfflineNoticeFlag();
}
