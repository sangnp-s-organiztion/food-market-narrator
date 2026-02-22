using food_market_narrator.Models;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;

namespace food_market_narrator.Services;

public interface IPOIService
{
    Task<List<POI>> GetPOIsAsync();
    Task<List<POI>> GetAllPOIsAsync();
    POI? GetNearestPOI(double currentLat, double currentLng);
    POI? UpdateNearestPOI(double currentLat, double currentLng);
    void HighlightNearestPOI(MauiMap map, POI? nearest);
}
