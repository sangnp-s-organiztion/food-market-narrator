using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Net.Http.Json;



namespace food_market_narrator.Services;

public class POIService : IPOIService
{
    private POI? _lastNearest;
    private bool _isInsidePOI = false;
    private List<POI>? _pois;

    // Danh sach cac POI
    private readonly HttpClient _httpClient;

    public POIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        Console.WriteLine($"[POIService] HttpClient.BaseAddress = {_httpClient.BaseAddress}");
    }

    public async Task<List<POI>> GetPOIsAsync()
    {
        if (_pois != null && _pois.Count > 0)
             return _pois;

        var baseCandidates = new List<string>();

        if (_httpClient.BaseAddress != null)
        {
            baseCandidates.Add(_httpClient.BaseAddress.ToString());
        }

        baseCandidates.AddRange(AppSettings.ApiFallbackBaseUrls);
        var uniqueBaseCandidates = baseCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var baseUrl in uniqueBaseCandidates)
        {
            try
            {
                var requestUrl = new Uri(new Uri(baseUrl), AppSettings.RestaurantEndpoint);
                Console.WriteLine($"[POIService] Trying URL = {requestUrl}");

                var data = await _httpClient.GetFromJsonAsync<List<POI>>(requestUrl);

                if (data == null)
                {
                    Console.WriteLine($"[POIService] Empty response from {requestUrl}");
                    continue;
                }

                _pois = data;
                Console.WriteLine($"[POIService] Loaded {_pois.Count} POIs from {requestUrl}");
                return _pois;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[POIService] Request failed: {baseUrl} -> {ex.Message}");
            }
        }

        Console.WriteLine("[POIService] Error fetching POIs from all candidates.");
        return new List<POI>();
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
        return GetNearestPOI(new Location(currentLat, currentLng), _pois);
    }

    public POI? GetNearestPOI(Location currentLocation, IEnumerable<POI>? pois = null)
    {
        var source = pois?.ToList() ?? _pois;
        if (source == null || source.Count == 0)
        {
            return null;
        }

        POI? nearest = null;
        double minDistance = double.MaxValue;

        foreach (var poi in source)
        {
            var distance = GetDistanceMeters(currentLocation, poi);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = poi;
            }
        }

        return nearest;
    }

    public double GetDistanceMeters(Location currentLocation, POI poi)
    {
        return Location.CalculateDistance(
            currentLocation,
            new Location(poi.Latitude, poi.Longitude),
            DistanceUnits.Kilometers) * 1000;
    }

    // Lấy POI gần nhất dựa trên vị trí hiện tại và các POIs
    public POI? UpdateNearestPOI(double currentLat, double currentLng)
    {
        if (_pois == null || !_pois.Any())
            return null;

        var currentLocation = new Location(currentLat, currentLng);

        var nearest = GetNearestPOI(currentLocation, _pois);

        if (nearest == null)
            return null;

        var minDistance = GetDistanceMeters(currentLocation, nearest);

        if (!_isInsidePOI)
        {
            // Chưa ở trong POI → xét EnterRadius
            if (minDistance <= AppSettings.PoiEnterRadiusMeters)
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
            if (nearest != _lastNearest && minDistance <= AppSettings.PoiEnterRadiusMeters)
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

                if (distanceFromLast > AppSettings.PoiExitRadiusMeters)
                {
                    _isInsidePOI = false;
                    _lastNearest = null;
                }
            }
        }

        return null; // Không có thay đổi
    }
}