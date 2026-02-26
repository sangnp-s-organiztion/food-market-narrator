using Microsoft.Maui.Devices.Sensors;

namespace food_market_narrator.Services;

public interface INarrationFlowService
{
    bool IsNarrating { get; }
    void StartNarration();
    Task CheckAndNarrateAsync(Location? currentLocation = null, bool force = false);
    void ResetPlayedPOIs();
}
