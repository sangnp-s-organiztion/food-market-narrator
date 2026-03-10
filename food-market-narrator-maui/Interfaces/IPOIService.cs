using food_market_narrator.Controls;
using food_market_narrator.Models;

namespace food_market_narrator.Services;

public interface IPOIService
{
    Task<List<POI>> GetPOIsAsync();
    Task<List<POI>> GetAllPOIsAsync();
    Task<POI?> GetPOIByIdAsync(string restaurantId);
    POI? GetNearestPOI(double currentLat, double currentLng);
    POI? UpdateNearestPOI(double currentLat, double currentLng);
    void HighlightNearestPOI(MapWebView map, POI? nearest);
}
