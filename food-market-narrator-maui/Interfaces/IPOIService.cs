using food_market_narrator.Models;
using Mapsui.UI.Maui;

namespace food_market_narrator.Services;

public interface IPOIService
{
    Task<List<POI>> GetPOIsAsync();
    Task<List<POI>> GetAllPOIsAsync();
    Task<POI?> GetPOIByIdAsync(string restaurantId);
    POI? GetNearestPOI(double currentLat, double currentLng);
    POI? GetNearestPOI(Location currentLocation, IEnumerable<POI>? pois = null);
    double GetDistanceMeters(Location currentLocation, POI poi);
    POI? UpdateNearestPOI(double currentLat, double currentLng);
    Task<List<DishModel>> GetDishesByRestaurantIdAsync(string restaurantId);
}
