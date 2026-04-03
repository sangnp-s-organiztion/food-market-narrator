namespace food_market_narrator.Services;

public interface ILocationLogSyncService
{
    string CurrentSessionId { get; }
    void Start();
    Task FlushNowAsync();
}
