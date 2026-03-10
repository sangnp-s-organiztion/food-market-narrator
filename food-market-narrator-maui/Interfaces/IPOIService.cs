using food_market_narrator.Models;
using Mapsui.UI.Maui;

namespace food_market_narrator.Services;

public interface IPOIService
{
    Task<List<POI>> GetPOIsAsync();
    Task<List<POI>> GetAllPOIsAsync();
    Task<POI?> GetPOIByIdAsync(string restaurantId);
    POI? GetNearestPOI(double currentLat, double currentLng);
    POI? UpdateNearestPOI(double currentLat, double currentLng);
    void HighlightNearestPOI(MapView mapView, POI? nearest);
}
