namespace food_market_narrator.Services;

public interface ILocationLogSyncService
{
    void Start();
    Task FlushNowAsync();
}
