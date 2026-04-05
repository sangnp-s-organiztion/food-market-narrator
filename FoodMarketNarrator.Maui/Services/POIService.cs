using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Net.Http.Json;
using System.Text.Json;
using System.Diagnostics;



namespace food_market_narrator.Services;

public class POIService : IPOIService
{
    private POI? _lastNearest;
    private bool _isInsidePOI = false;
    private List<POI>? _pois;
    private DateTime _lastFetchUtc = DateTime.MinValue;
    private bool _lastLoadSucceededFromNetwork;
    private static readonly TimeSpan PoiTtl = TimeSpan.FromMinutes(3);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
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
        Log($"[POIService] HttpClient.BaseAddress = {_httpClient.BaseAddress}");
    }

    private static void Log(string message)
    {
        Debug.WriteLine(message);
        Console.WriteLine(message);
    }

    public async Task<List<POI>> GetPOIsAsync()
    {
        if (_pois != null && _pois.Count > 0)
        {
            Log($"[POIService] Using in-memory POIs: {_pois.Count}");
            _lastLoadSucceededFromNetwork = false;
            return _pois;
        }

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
                Log($"[POIService] Trying URL = {requestUrl}");

                var data = await _httpClient.GetFromJsonAsync<List<POI>>(requestUrl);

                if (data == null)
                {
                    Log($"[POIService] Empty response from {requestUrl}");
                    continue;
                }

                _pois = data
                    .Where(p => p.IsActive)
                    .ToList();
                await SavePoisCacheAsync(_pois);
                _lastLoadSucceededFromNetwork = true;
                var totalAudios = _pois.Sum(p => p.Audios?.Count ?? 0);
                Log($"[POIService] Loaded {_pois.Count} POIs and {totalAudios} audios from {requestUrl}");
                return _pois;
            }
            catch (Exception ex)
            {
                Log($"[POIService] Request failed: {baseUrl} -> {ex.Message}");
            }
        }

        if (cachedPois.Count > 0)
        {
            _pois = cachedPois
                .Where(p => p.IsActive)
                .ToList();
            _lastLoadSucceededFromNetwork = false;
            var totalAudios = _pois.Sum(p => p.Audios?.Count ?? 0);
            Log($"[POIService] Loaded {_pois.Count} POIs and {totalAudios} audios from offline cache.");
            return _pois;
        }

        _lastLoadSucceededFromNetwork = false;
        Log("[POIService] Error fetching POIs from all candidates.");
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
        catch (Exception)
        {
            // Console.WriteLine($"[POIService] Read cache failed: {ex.Message}");
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
        catch (Exception)
        {
            // Console.WriteLine($"[POIService] Save cache failed: {ex.Message}");
        }
    }

    // Láº¥y táº¥t cáº£ cÃ¡c POIs Ä‘á»“ng bá»™
    public async Task<List<POI>> GetAllPOIsAsync()
    {
        var now = DateTime.UtcNow;
        var cacheAge = now - _lastFetchUtc;

        if (_pois != null && _pois.Any() && cacheAge < PoiTtl)
        {
            Log($"[POIService][TTL] cache-hit: age={cacheAge.TotalSeconds:F0}s < ttl={PoiTtl.TotalSeconds:F0}s, count={_pois.Count}");
            return _pois;
        }

        Log($"[POIService][TTL] cache-expired-or-empty: hasData={_pois != null && _pois.Any()}, age={cacheAge.TotalSeconds:F0}s, ttl={PoiTtl.TotalSeconds:F0}s");

        await _refreshLock.WaitAsync();
        try
        {
            now = DateTime.UtcNow;
            cacheAge = now - _lastFetchUtc;
            if (_pois != null && _pois.Any() && cacheAge < PoiTtl)
            {
                Log($"[POIService][TTL] cache-hit-after-lock: age={cacheAge.TotalSeconds:F0}s < ttl={PoiTtl.TotalSeconds:F0}s, count={_pois.Count}");
                return _pois;
            }

            var previous = _pois;
            var previousCount = previous?.Count ?? 0;

            // Bypass in-memory branch in GetPOIsAsync to trigger refresh attempt.
            _pois = null;
            Log($"[POIService][TTL] refreshing from source, previousCount={previousCount}");
            var refreshed = await GetPOIsAsync();

            if (refreshed != null && refreshed.Any())
            {
                // Only stamp TTL when network fetch succeeded.
                if (_lastLoadSucceededFromNetwork)
                {
                    _lastFetchUtc = DateTime.UtcNow;
                    Log($"[POIService][TTL] refresh-success-from-network: stampedAtUtc={_lastFetchUtc:O}, count={refreshed.Count}");
                }
                else
                {
                    Log($"[POIService][TTL] refresh-success-non-network: source=in-memory-or-offline, keepLastFetchUtc={_lastFetchUtc:O}, count={refreshed.Count}");
                }

                return refreshed;
            }

            if (previous != null && previous.Any())
            {
                _pois = previous;
                Log($"[POIService][TTL] refresh-empty -> restore-previous: restoredCount={previous.Count}, lastFetchUtc={_lastFetchUtc:O}");
                return previous;
            }

            Log("[POIService][TTL] refresh-empty-and-no-previous: returning empty list");
            return refreshed ?? new List<POI>();
        }
        finally
        {
            _refreshLock.Release();
        }
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

    // Láº¥y POI gáº§n nháº¥t dá»±a trÃªn vá»‹ trÃ­ hiá»‡n táº¡i vÃ  cÃ¡c POIs
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
            // ChÆ°a á»Ÿ trong POI â†’ xÃ©t EnterRadius
            if (minDistance <= AppSettings.PoiEnterRadiusMeters)
            {
                _isInsidePOI = true;
                _lastNearest = nearest;

                return nearest; // Trigger khi má»›i vÃ o
            }
        }
        else
        {
            // Äang á»Ÿ trong POI
            // Náº¿u Ä‘á»•i sang POI khÃ¡c vÃ  Ä‘á»§ gáº§n
            if (nearest != _lastNearest && minDistance <= AppSettings.PoiEnterRadiusMeters)
            {
                _lastNearest = nearest;
                return nearest; // Trigger POI má»›i
            }

            // Náº¿u Ä‘i xa khá»i POI hiá»‡n táº¡i > ExitRadius
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

        return null; // KhÃ´ng cÃ³ thay Ä‘á»•i
    }

    public void ResetGeofenceState()
    {
        _isInsidePOI = false;
        _lastNearest = null;
    }

    // Láº¥y danh sÃ¡ch mÃ³n Äƒn theo restaurant
    public async Task<List<DishModel>> GetDishesByRestaurantIdAsync(string restaurantId)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
        {
            return new List<DishModel>();
        }

        var baseUrl = AppSettings.ApiBaseUrl;
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return new List<DishModel>();
        }

        try
        {
            var url = $"{baseUrl.TrimEnd('/')}/Restaurant/{restaurantId}/dishes";
            System.Diagnostics.Debug.WriteLine($"[POIService] Requesting dishes from: {url}");
            var dishes = await _httpClient.GetFromJsonAsync<List<DishModel>>(url);

            if (dishes != null)
            {
                foreach (var dish in dishes)
                {
                    System.Diagnostics.Debug.WriteLine($"[POIService] Dish: {dish.Name}, ImageFileName: {dish.ImageFileName}");
                }
            }

            return dishes ?? new List<DishModel>();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[POIService] GetDishes failed: {ex.Message}");
            return new List<DishModel>();
        }
    }
}

