using food_market_narrator.Models;
using System.Net.Http.Json;



namespace food_market_narrator.Services;

public class POIService : IPOIService
{
    private POI? _lastNearest;
    private bool _isInsidePOI = false;
    private List<POI>? _pois;
    private const double EnterRadius = 30; // mét
    private const double ExitRadius = 40;  // mét

    // Danh sach cac POI
    private readonly HttpClient _httpClient;

    public POIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<POI>> GetPOIsAsync()
    {
        if (_pois != null && _pois.Count > 0)
             return _pois;

        try
        {
            var url = "http://10.0.2.2:5044/api/restaurant";
            // Nếu chạy Windows local app thì dùng localhost

            var data = await _httpClient.GetFromJsonAsync<List<POI>>(url);

            if (data == null)
                return new List<POI>();

            _pois = data; // Cache the data
            return _pois;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching POIs: {ex.Message}");
            return new List<POI>();
        }
    }

    // Lấy tất cả các POIs đồng bộ
    public async Task<List<POI>> GetAllPOIsAsync()
    {
        if (_pois == null || !_pois.Any())
        {
            return await GetPOIsAsync();
        }
        return _pois;
    }

    public async Task<POI?> GetPOIByIdAsync(string restaurantId)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
        {
            return null;
        }

        var pois = await GetAllPOIsAsync();
        return pois.FirstOrDefault(p =>
            string.Equals(p.restaurantId, restaurantId, StringComparison.OrdinalIgnoreCase));
    }

    public POI? GetNearestPOI(double currentLat, double currentLng)
    {
        if (_pois == null || !_pois.Any())
            return null;

        var currentLocation = new Location(currentLat, currentLng);

        return _pois
            .OrderBy(poi => Location.CalculateDistance(
                currentLocation,
                new Location(poi.Latitude, poi.Longitude),
                DistanceUnits.Kilometers))
            .FirstOrDefault();
    }

    // Lấy POI gần nhất dựa trên vị trí hiện tại và các POIs
    public POI? UpdateNearestPOI(double currentLat, double currentLng)
    {
        if (_pois == null || !_pois.Any())
            return null;

        var currentLocation = new Location(currentLat, currentLng);

        POI? nearest = null;
        double minDistance = double.MaxValue;

        // Tìm POI gần nhất
        foreach (var poi in _pois)
        {
            var poiLocation = new Location(poi.Latitude, poi.Longitude);

            var distance = Location.CalculateDistance(
                currentLocation,
                poiLocation,
                DistanceUnits.Kilometers) * 1000; // đổi sang mét

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = poi;
            }
        }

        if (nearest == null)
            return null;

        if (!_isInsidePOI)
        {
            // Chưa ở trong POI → xét EnterRadius
            if (minDistance <= EnterRadius)
            {
                _isInsidePOI = true;
                _lastNearest = nearest;

                return nearest; // Trigger khi mới vào
            }
        }
        else
        {
            // Đang ở trong POI
            // Nếu đổi sang POI khác và đủ gần
            if (nearest != _lastNearest && minDistance <= EnterRadius)
            {
                _lastNearest = nearest;
                return nearest; // Trigger POI mới
            }

            // Nếu đi xa khỏi POI hiện tại > ExitRadius
            if (_lastNearest != null)
            {
                var lastLocation = new Location(
                    _lastNearest.Latitude,
                    _lastNearest.Longitude);

                var distanceFromLast = Location.CalculateDistance(
                    currentLocation,
                    lastLocation,
                    DistanceUnits.Kilometers) * 1000;

                if (distanceFromLast > ExitRadius)
                {
                    _isInsidePOI = false;
                    _lastNearest = null;
                }
            }
        }

        return null; // Không có thay đổi
    }
}