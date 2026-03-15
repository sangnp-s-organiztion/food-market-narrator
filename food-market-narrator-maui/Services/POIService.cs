using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Net.Http.Json;
using System.Text.Json;



namespace food_market_narrator.Services;

public class POIService : IPOIService
{
    private POI? _lastNearest;
    private bool _isInsidePOI = false;
    private List<POI>? _pois;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

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

        var cachedPois = await ReadPoisCacheAsync();

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
                await SavePoisCacheAsync(_pois);
                Console.WriteLine($"[POIService] Loaded {_pois.Count} POIs from {requestUrl}");
                return _pois;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[POIService] Request failed: {baseUrl} -> {ex.Message}");
            }
        }

        if (cachedPois.Count > 0)
        {
            _pois = cachedPois;
            Console.WriteLine($"[POIService] Loaded {_pois.Count} POIs from offline cache.");
            return _pois;
        }

        Console.WriteLine("[POIService] Error fetching POIs from all candidates.");
        return new List<POI>();
    }

    private static string GetPoiCacheFilePath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "offline_cache");
        Directory.CreateDirectory(cacheDir);
        return Path.Combine(cacheDir, "pois.json");
    }

    private static async Task<List<POI>> ReadPoisCacheAsync()
    {
        var path = GetPoiCacheFilePath();
        if (!File.Exists(path))
        {
            return new List<POI>();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<List<POI>>(stream, JsonOptions);
            return data ?? new List<POI>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[POIService] Read cache failed: {ex.Message}");
            return new List<POI>();
        }
    }

    private static async Task SavePoisCacheAsync(List<POI> pois)
    {
        try
        {
            var path = GetPoiCacheFilePath();
            var tempPath = $"{path}.tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, pois, JsonOptions);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[POIService] Save cache failed: {ex.Message}");
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
        return GetNearestPOI(new Location(currentLat, currentLng), _pois);
    }

    public POI? GetNearestPOI(Location currentLocation, IEnumerable<POI>? pois = null)
    {
        var source = pois?.ToList() ?? _pois;
        if (source == null || source.Count == 0)
        {
            return null;
        }

        return source
            .OrderBy(poi => GetDistanceMeters(currentLocation, poi))
            .FirstOrDefault();
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