using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace food_market_narrator.Services;

// Service lấy dữ liệu tour theo chiến lược cache-first + refresh nền, kèm chuẩn hóa ảnh.
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
    private string? _lastSuccessfulBaseUrl;

    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan TourRequestTimeout = TimeSpan.FromSeconds(5);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    // Khởi tạo service tour với HttpClient và LocationService để build endpoint theo vị trí hiện tại.
    public TourService(HttpClient httpClient, ILocationService locationService)
    {
        _httpClient = httpClient;
        _locationService = locationService;
    }

    // Lấy danh sách tour: ưu tiên memory cache -> disk cache -> network.
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

    // Lấy chi tiết 1 tour theo id, vẫn giữ fallback cache nếu mạng lỗi.
    public async Task<TourModel?> GetTourByIdAsync(int tourId)
    {
        var cachedTour = _cachedTours?.FirstOrDefault(x => x.TourId == tourId);
        if (cachedTour == null)
        {
            var diskCache = await ReadToursCacheAsync();
            if (diskCache.Count > 0)
            {
                SetMemoryCache(diskCache);
                cachedTour = _cachedTours?.FirstOrDefault(x => x.TourId == tourId);
            }
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            return cachedTour == null ? null : NormalizeTour(cachedTour);
        }

        if (cachedTour != null)
        {
            _ = RefreshTourByIdInBackgroundAsync(tourId);
            return NormalizeTour(cachedTour);
        }

        try
        {
            var location = _locationService.LastKnownLocation;
            var refreshedTour = await LoadTourByIdFromNetworkAsync(tourId, location);
            if (refreshedTour != null)
            {
                return refreshedTour;
            }
        }
        catch
        {
            // Return cached fallback below.
        }

        return cachedTour == null ? null : NormalizeTour(cachedTour);
    }

    // Kiểm tra memory cache còn trong TTL.
    private bool HasFreshMemoryCache()
    {
        return _cachedTours != null
            && _cachedTours.Count > 0
            && DateTime.UtcNow - _memoryCachedAtUtc < CacheTtl;
    }

    // Quyết định có nên refresh từ network hay chưa.
    private bool ShouldRefreshFromNetwork()
    {
        return DateTime.UtcNow - _lastNetworkFetchUtc >= CacheTtl;
    }

    // Ghi đè memory cache bằng dữ liệu đã normalize.
    private void SetMemoryCache(List<TourModel> tours)
    {
        _cachedTours = NormalizeTours(tours);
        _memoryCachedAtUtc = DateTime.UtcNow;
    }

    // Refresh danh sách tour nền, lỗi sẽ bị bỏ qua để không ảnh hưởng luồng UI đang dùng cache.
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

    // Điều phối refresh từ network bằng lock để tránh nhiều request đồng thời.
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

    // Tải danh sách tour từ nhiều base URL ứng viên, fallback cache nếu tất cả đều lỗi.
    private async Task<List<TourModel>> LoadToursFromNetworkAsync(List<TourModel> fallbackTours)
    {
        var location = _locationService.LastKnownLocation;

        foreach (var baseUrl in BuildBaseUrlCandidates())
        {
            try
            {
                var endpoint = BuildTourEndpoint(baseUrl, location);

                using var cts = new CancellationTokenSource(TourRequestTimeout);
                var tours = await _httpClient.GetFromJsonAsync<List<TourModel>>(endpoint, cts.Token);
                if (tours == null || tours.Count == 0)
                {
                    continue;
                }

                _lastSuccessfulBaseUrl = baseUrl;
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

    // Build danh sách base URL có ưu tiên endpoint đã thành công gần nhất.
    private IEnumerable<string> BuildBaseUrlCandidates()
    {
        var baseCandidates = new List<string>();

        if (!string.IsNullOrWhiteSpace(_lastSuccessfulBaseUrl))
        {
            baseCandidates.Add(_lastSuccessfulBaseUrl);
        }

        if (_httpClient.BaseAddress != null)
        {
            baseCandidates.Add(_httpClient.BaseAddress.ToString());
        }

        baseCandidates.AddRange(AppSettings.ApiFallbackBaseUrls);

        return baseCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim().TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    // Refresh chi tiết 1 tour ở nền khi đang có cache để giữ dữ liệu mới nhất.
    private async Task RefreshTourByIdInBackgroundAsync(int tourId)
    {
        if (!await _networkRefreshLock.WaitAsync(0))
        {
            return;
        }

        try
        {
            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                return;
            }

            var location = _locationService.LastKnownLocation;
            await LoadTourByIdFromNetworkAsync(tourId, location);
        }
        catch
        {
            // Keep cached detail visible even if background refresh fails.
        }
        finally
        {
            _networkRefreshLock.Release();
        }
    }

    // Tải chi tiết tour theo id từ network, normalize và merge vào cache khi thành công.
    private async Task<TourModel?> LoadTourByIdFromNetworkAsync(int tourId, Location? location)
    {
        foreach (var baseUrl in BuildBaseUrlCandidates())
        {
            try
            {
                var endpoint = BuildTourDetailEndpoint(baseUrl, tourId, location);
                using var cts = new CancellationTokenSource(TourRequestTimeout);
                var tour = await _httpClient.GetFromJsonAsync<TourModel>(endpoint, cts.Token);
                if (tour == null)
                {
                    continue;
                }

                _lastSuccessfulBaseUrl = baseUrl;
                var normalizedTour = NormalizeTour(tour);
                await MergeTourIntoCacheAsync(normalizedTour);
                return normalizedTour;
            }
            catch
            {
                // Fall through to try next candidate.
            }
        }

        return null;
    }

    // Build endpoint danh sách tour, có gắn lat/lng/radius khi đã có vị trí người dùng.
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

    // Build endpoint gọi chi tiết 1 tour, có kèm vị trí hiện tại khi có.
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

    // Resolve URL ảnh hiển thị của tour, ưu tiên ảnh truyền vào, fallback từ stop đầu tiên hợp lệ.
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

    // Trả về đường dẫn file cache tours.json và đảm bảo thư mục cache tồn tại.
    private static string GetToursCacheFilePath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, OfflineCacheFolderName);
        Directory.CreateDirectory(cacheDir);
        return Path.Combine(cacheDir, "tours.json");
    }

    // Đọc tour cache từ disk và normalize dữ liệu trước khi dùng.
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

    // Ghi danh sách tour xuống disk theo cơ chế temp file -> replace để tránh hỏng file.
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

    // Merge một tour mới/cập nhật vào cache hiện có rồi ghi xuống disk.
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

    // Normalize toàn bộ danh sách tour: stops + ảnh stop + ảnh đại diện tour.
    private List<TourModel> NormalizeTours(List<TourModel> tours)
    {
        NormalizeStops(tours);
        foreach (var tour in tours)
        {
            NormalizeStopImages(tour);
            tour.ResolvedImageUrl = ResolveImageUrl(tour.ImageUrl, tour.Stops);
        }

        return tours;
    }

    // Normalize một tour đơn lẻ trước khi trả về cho UI.
    private TourModel NormalizeTour(TourModel tour)
    {
        tour.Stops ??= new List<TourStopModel>();
        NormalizeStopImages(tour);
        tour.ResolvedImageUrl = ResolveImageUrl(tour.ImageUrl, tour.Stops);
        return tour;
    }

    // Chuẩn hóa URL ảnh cho từng stop trong tour.
    private void NormalizeStopImages(TourModel tour)
    {
        foreach (var stop in tour.Stops)
        {
            stop.PrimaryImageUrl = ResolveImageUrl(stop.PrimaryImageUrl, null);
        }
    }

    // Đảm bảo mọi tour đều có collection Stops không null để tránh null-check lặp lại.
    private static List<TourModel> NormalizeStops(List<TourModel> tours)
    {
        foreach (var tour in tours)
        {
            tour.Stops ??= new List<TourStopModel>();
        }

        return tours;
    }

    // Ưu tiên trả path ảnh local/cache nếu có, fallback về nguồn đầu tiên hợp lệ.
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

    // Sinh path cache ảnh từ hash URL nguồn để dedupe tên file.
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

    // Lấy thư mục cache ảnh và tạo mới nếu chưa tồn tại.
    private static string GetImageCacheRootPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, ImageCacheFolderName);
        Directory.CreateDirectory(path);
        return path;
    }

    // Kiểm tra file ảnh cache có tồn tại và đủ dung lượng tối thiểu.
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
