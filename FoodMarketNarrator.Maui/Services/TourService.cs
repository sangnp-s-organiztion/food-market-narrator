using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace food_market_narrator.Services;

public class TourService : ITourService
{
    private const string OfflineCacheFolderName = "offline_cache";
    private const string ImageCacheFolderName = "image_cache";
    private const int MinValidImageBytes = 128;

    private readonly HttpClient _httpClient;
    private readonly ILocationService _locationService;
    private readonly SemaphoreSlim _cacheFileLock = new(1, 1);
    private readonly SemaphoreSlim _networkRefreshLock = new(1, 1);
    private List<TourModel>? _cachedTours;
    private DateTime _memoryCachedAtUtc = DateTime.MinValue;
    private DateTime _lastNetworkFetchUtc = DateTime.MinValue;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan TourRequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public TourService(HttpClient httpClient, ILocationService locationService)
    {
        _httpClient = httpClient;
        _locationService = locationService;
    }

    public async Task<List<TourModel>> GetToursAsync()
    {
        if (HasFreshMemoryCache())
        {
            return _cachedTours!;
        }

        var cachedTours = await ReadToursCacheAsync();
        if (cachedTours.Count > 0)
        {
            SetMemoryCache(cachedTours);

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet && ShouldRefreshFromNetwork())
            {
                _ = RefreshToursInBackgroundAsync();
            }

            return _cachedTours!;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return new List<TourModel>();
        }

        return await RefreshToursFromNetworkAsync(new List<TourModel>());
    }

    public async Task<TourModel?> GetTourByIdAsync(int tourId)
    {
        var cachedTour = _cachedTours?.FirstOrDefault(x => x.TourId == tourId);
        if (cachedTour == null)
        {
            var diskCache = await ReadToursCacheAsync();
            cachedTour = diskCache.FirstOrDefault(x => x.TourId == tourId);
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return cachedTour == null ? null : NormalizeTour(cachedTour);
        }

        try
        {
            var location = _locationService.LastKnownLocation;

            foreach (var baseUrl in BuildBaseUrlCandidates())
            {
                try
                {
                    var endpoint = BuildTourDetailEndpoint(baseUrl.TrimEnd('/'), tourId, location);
                    using var cts = new CancellationTokenSource(TourRequestTimeout);
                    var tour = await _httpClient.GetFromJsonAsync<TourModel>(endpoint, cts.Token);
                    if (tour == null)
                    {
                        continue;
                    }

                    var normalizedTour = NormalizeTour(tour);
                    await MergeTourIntoCacheAsync(normalizedTour);
                    return normalizedTour;
                }
                catch
                {
                    // Fall through to try next candidate.
                }
            }
        }
        catch
        {
            // Return cached fallback below.
        }

        return cachedTour == null ? null : NormalizeTour(cachedTour);
    }

    private bool HasFreshMemoryCache()
    {
        return _cachedTours != null
            && _cachedTours.Count > 0
            && DateTime.UtcNow - _memoryCachedAtUtc < CacheTtl;
    }

    private bool ShouldRefreshFromNetwork()
    {
        return DateTime.UtcNow - _lastNetworkFetchUtc >= CacheTtl;
    }

    private void SetMemoryCache(List<TourModel> tours)
    {
        _cachedTours = NormalizeTours(tours);
        _memoryCachedAtUtc = DateTime.UtcNow;
    }

    private async Task RefreshToursInBackgroundAsync()
    {
        try
        {
            await RefreshToursFromNetworkAsync(_cachedTours ?? new List<TourModel>());
        }
        catch
        {
            // Disk cache is already usable; background refresh failures are non-critical.
        }
    }

    private async Task<List<TourModel>> RefreshToursFromNetworkAsync(List<TourModel> fallbackTours)
    {
        if (!await _networkRefreshLock.WaitAsync(0))
        {
            return fallbackTours;
        }

        try
        {
            if (!ShouldRefreshFromNetwork() && _cachedTours != null && _cachedTours.Count > 0)
            {
                return _cachedTours;
            }

            return await LoadToursFromNetworkAsync(fallbackTours);
        }
        finally
        {
            _networkRefreshLock.Release();
        }
    }

    private async Task<List<TourModel>> LoadToursFromNetworkAsync(List<TourModel> fallbackTours)
    {
        var location = _locationService.LastKnownLocation;

        foreach (var baseUrl in BuildBaseUrlCandidates())
        {
            try
            {
                var endpoint = BuildTourEndpoint(baseUrl.TrimEnd('/'), location);

                using var cts = new CancellationTokenSource(TourRequestTimeout);
                var tours = await _httpClient.GetFromJsonAsync<List<TourModel>>(endpoint, cts.Token);
                if (tours == null || tours.Count == 0)
                {
                    continue;
                }

                SetMemoryCache(tours);
                _lastNetworkFetchUtc = DateTime.UtcNow;
                await SaveToursCacheAsync(_cachedTours!);
                return _cachedTours!;
            }
            catch
            {
                // Fall through to try next candidate.
            }
        }

        if (fallbackTours.Count > 0)
        {
            SetMemoryCache(fallbackTours);
            return _cachedTours!;
        }

        return new List<TourModel>();
    }

    private IEnumerable<string> BuildBaseUrlCandidates()
    {
        var baseCandidates = new List<string>();
        if (_httpClient.BaseAddress != null)
        {
            baseCandidates.Add(_httpClient.BaseAddress.ToString());
        }

        baseCandidates.AddRange(AppSettings.ApiFallbackBaseUrls);

        return baseCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    private static string BuildTourEndpoint(string baseUrl, Location? location)
    {
        if (location == null)
        {
            return $"{baseUrl}/{AppSettings.TourEndpoint}";
        }

        var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
        var radius = AppSettings.PoiEnterRadiusMeters.ToString(CultureInfo.InvariantCulture);
        return $"{baseUrl}/{AppSettings.TourEndpoint}?latitude={lat}&longitude={lng}&radiusMeters={radius}";
    }

    private static string BuildTourDetailEndpoint(string baseUrl, int tourId, Location? location)
    {
        if (location == null)
        {
            return $"{baseUrl}/{AppSettings.TourEndpoint}/{tourId}";
        }

        var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
        var radius = AppSettings.PoiEnterRadiusMeters.ToString(CultureInfo.InvariantCulture);
        return $"{baseUrl}/{AppSettings.TourEndpoint}/{tourId}?latitude={lat}&longitude={lng}&radiusMeters={radius}";
    }

    private string ResolveImageUrl(string? imageUrl, List<TourStopModel>? stops)
    {
        var source = imageUrl;
        if (string.IsNullOrWhiteSpace(source))
        {
            source = stops?
                .OrderBy(s => s.StopOrder)
                .Select(s => s.PrimaryImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return "dotnet_bot.svg";
        }

        var originalSource = source.Trim();
        string resolved;
        if (Uri.TryCreate(source, UriKind.Absolute, out _))
        {
            resolved = source;
        }
        else if (source.StartsWith("/", StringComparison.Ordinal))
        {
            resolved = $"{_httpClient.BaseAddress?.ToString().TrimEnd('/')}{source}";
        }
        else if (source.Contains("/", StringComparison.Ordinal))
        {
            resolved = source;
        }
        else
        {
            resolved = source.Trim();
        }

        return ResolveCachedImagePath(originalSource, resolved);
    }

    private static string GetToursCacheFilePath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, OfflineCacheFolderName);
        Directory.CreateDirectory(cacheDir);
        return Path.Combine(cacheDir, "tours.json");
    }

    private async Task<List<TourModel>> ReadToursCacheAsync()
    {
        await _cacheFileLock.WaitAsync();
        try
        {
            var path = GetToursCacheFilePath();
            if (!File.Exists(path))
            {
                return new List<TourModel>();
            }

            try
            {
                await using var stream = File.OpenRead(path);
                var data = await JsonSerializer.DeserializeAsync<List<TourModel>>(stream, JsonOptions);
                return data == null ? new List<TourModel>() : NormalizeStops(data);
            }
            catch
            {
                return new List<TourModel>();
            }
        }
        finally
        {
            _cacheFileLock.Release();
        }
    }

    private async Task SaveToursCacheAsync(List<TourModel> tours)
    {
        await _cacheFileLock.WaitAsync();
        try
        {
            var path = GetToursCacheFilePath();
            var tempPath = $"{path}.{Guid.NewGuid():N}.tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, tours, JsonOptions);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
        catch
        {
            // Ignore cache-write failures silently.
        }
        finally
        {
            _cacheFileLock.Release();
        }
    }

    private async Task MergeTourIntoCacheAsync(TourModel tour)
    {
        var tours = await ReadToursCacheAsync();
        var index = tours.FindIndex(x => x.TourId == tour.TourId);
        if (index >= 0)
        {
            tours[index] = tour;
        }
        else
        {
            tours.Add(tour);
        }

        SetMemoryCache(tours);
        await SaveToursCacheAsync(_cachedTours!);
    }

    private List<TourModel> NormalizeTours(List<TourModel> tours)
    {
        NormalizeStops(tours);
        foreach (var tour in tours)
        {
            tour.ResolvedImageUrl = ResolveImageUrl(tour.ImageUrl, tour.Stops);
        }

        return tours;
    }

    private TourModel NormalizeTour(TourModel tour)
    {
        tour.Stops ??= new List<TourStopModel>();
        tour.ResolvedImageUrl = ResolveImageUrl(tour.ImageUrl, tour.Stops);
        return tour;
    }

    private static List<TourModel> NormalizeStops(List<TourModel> tours)
    {
        foreach (var tour in tours)
        {
            tour.Stops ??= new List<TourStopModel>();
        }

        return tours;
    }

    private static string ResolveCachedImagePath(params string?[] sources)
    {
        foreach (var source in sources)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            if (File.Exists(source))
            {
                return source;
            }

            var cachedPath = GetImageCachePath(source);
            if (IsValidImageFile(cachedPath))
            {
                return cachedPath;
            }
        }

        return sources.FirstOrDefault(x => !string.IsNullOrWhiteSpace(x))?.Trim() ?? "dotnet_bot.svg";
    }

    private static string GetImageCachePath(string source)
    {
        var normalized = source.Replace("\\", "/", StringComparison.Ordinal).Trim().ToLowerInvariant();
        var ext = Path.GetExtension(normalized);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".img";
        }

        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(GetImageCacheRootPath(), $"{hash}{ext}");
    }

    private static string GetImageCacheRootPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, ImageCacheFolderName);
        Directory.CreateDirectory(path);
        return path;
    }

    private static bool IsValidImageFile(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            return new FileInfo(path).Length >= MinValidImageBytes;
        }
        catch
        {
            return false;
        }
    }
}
