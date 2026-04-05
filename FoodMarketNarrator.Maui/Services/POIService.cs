using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics;



namespace food_market_narrator.Services;

public class POIService : IPOIService
{
    private enum WarmupJobKind
    {
        Image = 0,
        Dishes = 1
    }

    private sealed record WarmupJob(string Key, WarmupJobKind Kind, string Value, int Priority);

    private POI? _lastNearest;
    private bool _isInsidePOI = false;
    private List<POI>? _pois;
    private DateTime _lastFetchUtc = DateTime.MinValue;
    private DateTime _lastFetchFailureUtc = DateTime.MinValue;
    private int _consecutiveFetchFailures;
    private bool _lastLoadSucceededFromNetwork;
    private static readonly TimeSpan PoiTtl = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan FetchFailureCooldown = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan RestaurantRequestTimeout = TimeSpan.FromSeconds(5);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly SemaphoreSlim _offlineWarmupLock = new(1, 1);
    private Task? _offlineWarmupTask;
    private readonly object _warmupQueueLock = new();
    private readonly PriorityQueue<WarmupJob, int> _warmupQueue = new();
    private readonly SemaphoreSlim _warmupQueueSignal = new(0);
    private readonly ConcurrentDictionary<string, byte> _queuedOrRunningWarmupKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Task<string?>> _imageDownloadsInFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Task<List<DishModel>>> _dishRequestsInFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileWriteLocks = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _imageWarmupLimiter = new(2, 2);
    private readonly SemaphoreSlim _dishWarmupLimiter = new(1, 1);
    private int _warmupWorkersStarted;
    private const string ImageCacheFolderName = "image_cache";
    private const int MinValidImageBytes = 128;
    private const int WarmupWorkerCount = 3;
    private const int WarmupPhaseATopCount = 6;
    private const int WarmupPriorityHigh = 0;
    private const int WarmupPriorityNormal = 1;
    private static readonly TimeSpan WarmupPhaseBDelay = TimeSpan.FromSeconds(4);
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

    private static string FormatException(Exception ex)
    {
        var inner = ex.InnerException?.Message;
        return $"{ex.GetType().Name}: {ex.Message}" + (string.IsNullOrWhiteSpace(inner) ? string.Empty : $" | Inner: {inner}");
    }

    public async Task<List<POI>> GetPOIsAsync()
    {
        var swTotal = Stopwatch.StartNew();
        if (_pois != null && _pois.Count > 0)
        {
            ApplyCachedImagePaths(_pois);
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

        Log($"[POIService] Candidate endpoints ({uniqueBaseCandidates.Count}): {string.Join(" | ", uniqueBaseCandidates)}");

        foreach (var baseUrl in uniqueBaseCandidates)
        {
            try
            {
                var requestUrl = new Uri(new Uri(baseUrl), AppSettings.RestaurantEndpoint);
                using var cts = new CancellationTokenSource(RestaurantRequestTimeout);
                var sw = Stopwatch.StartNew();
                Log($"[POIService] Trying URL = {requestUrl} | timeout={RestaurantRequestTimeout.TotalSeconds:F0}s");

                var data = await _httpClient.GetFromJsonAsync<List<POI>>(requestUrl, cts.Token);

                if (data == null)
                {
                    Log($"[POIService] Empty response from {requestUrl} | elapsedMs={sw.ElapsedMilliseconds}");
                    continue;
                }

                _pois = data
                    .Where(p => p.IsActive)
                    .ToList();
                ApplyCachedImagePaths(_pois);
                await SavePoisCacheAsync(_pois);
                StartOfflineAssetWarmup(_pois);
                _lastLoadSucceededFromNetwork = true;
                _consecutiveFetchFailures = 0;
                _lastFetchFailureUtc = DateTime.MinValue;
                var totalAudios = _pois.Sum(p => p.Audios?.Count ?? 0);
                Log($"[POIService] Loaded {_pois.Count} POIs and {totalAudios} audios from {requestUrl} | elapsedMs={sw.ElapsedMilliseconds} | totalElapsedMs={swTotal.ElapsedMilliseconds}");
                return _pois;
            }
            catch (Exception ex)
            {
                Log($"[POIService] Request failed: {baseUrl} -> {FormatException(ex)}");
            }
        }

        if (cachedPois.Count > 0)
        {
            _pois = cachedPois
                .Where(p => p.IsActive)
                .ToList();
            ApplyCachedImagePaths(_pois);
            StartOfflineAssetWarmup(_pois);
            _lastLoadSucceededFromNetwork = false;
            _consecutiveFetchFailures++;
            _lastFetchFailureUtc = DateTime.UtcNow;
            var totalAudios = _pois.Sum(p => p.Audios?.Count ?? 0);
            Log($"[POIService] Loaded {_pois.Count} POIs and {totalAudios} audios from offline cache. | failures={_consecutiveFetchFailures} | elapsedMs={swTotal.ElapsedMilliseconds}");
            return _pois;
        }

        _lastLoadSucceededFromNetwork = false;
        _consecutiveFetchFailures++;
        _lastFetchFailureUtc = DateTime.UtcNow;
        Log($"[POIService] Error fetching POIs from all candidates. | failures={_consecutiveFetchFailures} | elapsedMs={swTotal.ElapsedMilliseconds}");
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

    private static string GetOfflineCacheRootPath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "offline_cache");
        Directory.CreateDirectory(cacheDir);
        return cacheDir;
    }

    private static string GetDishesCacheFilePath(string restaurantId)
    {
        var dishesDir = Path.Combine(GetOfflineCacheRootPath(), "dishes");
        Directory.CreateDirectory(dishesDir);
        var safeId = string.Join("_", restaurantId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(dishesDir, $"{safeId}.json");
    }

    private static async Task<List<DishModel>> ReadDishesCacheAsync(string restaurantId)
    {
        var path = GetDishesCacheFilePath(restaurantId);
        if (!File.Exists(path))
        {
            return new List<DishModel>();
        }

        try
        {
            await using var stream = File.OpenRead(path);
            var data = await JsonSerializer.DeserializeAsync<List<DishModel>>(stream, JsonOptions);
            return data ?? new List<DishModel>();
        }
        catch
        {
            return new List<DishModel>();
        }
    }

    private async Task SaveDishesCacheAsync(string restaurantId, List<DishModel> dishes)
    {
        var path = GetDishesCacheFilePath(restaurantId);
        var fileLock = GetFileWriteLock(path);
        await fileLock.WaitAsync();
        try
        {
            var tempPath = $"{path}.tmp";

            await using (var stream = File.Create(tempPath))
            {
                await JsonSerializer.SerializeAsync(stream, dishes, JsonOptions);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }

            File.Move(tempPath, path);
        }
        catch
        {
            // Ignore cache-write failures.
        }
        finally
        {
            fileLock.Release();
        }
    }

    private static string GetImageCacheRootPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, ImageCacheFolderName);
        Directory.CreateDirectory(path);
        return path;
    }

    private static string GetImageCachePath(string source)
    {
        var normalized = source.Replace("\\", "/", StringComparison.Ordinal).Trim();
        var ext = Path.GetExtension(normalized);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".img";
        }

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized.ToLowerInvariant())));
        return Path.Combine(GetImageCacheRootPath(), $"{hash}{ext}");
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

    private void StartOfflineAssetWarmup(List<POI> pois)
    {
        if (pois.Count == 0)
        {
            return;
        }

        if (_offlineWarmupTask != null && !_offlineWarmupTask.IsCompleted)
        {
            return;
        }

        _offlineWarmupTask = Task.Run(async () =>
        {
            await _offlineWarmupLock.WaitAsync();
            try
            {
                EnsureWarmupWorkersStarted();
                Log($"[POIService][Offline] Warm-up scheduling start for {pois.Count} POIs");

                var phaseA = pois.Take(WarmupPhaseATopCount).ToList();
                EnqueuePoiWarmupJobs(phaseA, WarmupPriorityHigh, includeAllImages: false);

                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(WarmupPhaseBDelay);
                        EnqueuePoiWarmupJobs(pois, WarmupPriorityNormal, includeAllImages: true);
                        Log("[POIService][Offline] Warm-up phase B queued");
                    }
                    catch (Exception ex)
                    {
                        Log($"[POIService][Offline] Warm-up phase B scheduling failed: {ex.Message}");
                    }
                });

                Log("[POIService][Offline] Warm-up phase A queued");
            }
            catch (Exception ex)
            {
                Log($"[POIService][Offline] Warm-up failed: {ex.Message}");
            }
            finally
            {
                _offlineWarmupLock.Release();
            }
        });
    }

    private void EnsureWarmupWorkersStarted()
    {
        if (Interlocked.Exchange(ref _warmupWorkersStarted, 1) == 1)
        {
            return;
        }

        for (var i = 0; i < WarmupWorkerCount; i++)
        {
            _ = Task.Run(WarmupWorkerLoopAsync);
        }
    }

    private async Task WarmupWorkerLoopAsync()
    {
        while (true)
        {
            await _warmupQueueSignal.WaitAsync();

            WarmupJob? job = null;
            lock (_warmupQueueLock)
            {
                if (_warmupQueue.Count > 0)
                {
                    job = _warmupQueue.Dequeue();
                }
            }

            if (job == null)
            {
                continue;
            }

            try
            {
                switch (job.Kind)
                {
                    case WarmupJobKind.Image:
                        await _imageWarmupLimiter.WaitAsync();
                        try
                        {
                            await EnsureImageCachedWithDedupeAsync(job.Value);
                        }
                        finally
                        {
                            _imageWarmupLimiter.Release();
                        }
                        break;

                    case WarmupJobKind.Dishes:
                        await _dishWarmupLimiter.WaitAsync();
                        try
                        {
                            var dishes = await GetDishesByRestaurantIdAsync(job.Value);
                            if (dishes.Count > 0 && _pois != null)
                            {
                                var poi = _pois.FirstOrDefault(x => string.Equals(x.restaurantId, job.Value, StringComparison.OrdinalIgnoreCase));
                                if (poi != null)
                                {
                                    poi.Dishes = dishes;
                                }
                            }
                        }
                        finally
                        {
                            _dishWarmupLimiter.Release();
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Log($"[POIService][Offline] Warm-up job failed ({job.Key}): {ex.Message}");
            }
            finally
            {
                _queuedOrRunningWarmupKeys.TryRemove(job.Key, out _);
            }
        }
    }

    private void EnqueuePoiWarmupJobs(IEnumerable<POI> pois, int priority, bool includeAllImages)
    {
        foreach (var poi in pois)
        {
            if (!string.IsNullOrWhiteSpace(poi.restaurantId))
            {
                EnqueueWarmupJob(new WarmupJob(
                    $"dish:{poi.restaurantId}",
                    WarmupJobKind.Dishes,
                    poi.restaurantId,
                    priority));
            }

            if (poi.Images == null || poi.Images.Count == 0)
            {
                continue;
            }

            var imageCandidates = poi.Images
                .OrderByDescending(x => x.IsPrimary)
                .ThenBy(x => x.SortOrder)
                .Where(x => !string.IsNullOrWhiteSpace(x.ImageUrl))
                .ToList();

            if (!includeAllImages)
            {
                imageCandidates = imageCandidates.Take(1).ToList();
            }

            foreach (var image in imageCandidates)
            {
                if (string.IsNullOrWhiteSpace(image.ImageUrl))
                {
                    continue;
                }

                if (File.Exists(image.ImageUrl))
                {
                    continue;
                }

                var cachedPath = GetImageCachePath(image.ImageUrl);
                if (IsValidImageFile(cachedPath))
                {
                    continue;
                }

                if (!IsRemoteImageCandidate(image.ImageUrl))
                {
                    continue;
                }

                var normalized = image.ImageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim().ToLowerInvariant();
                EnqueueWarmupJob(new WarmupJob(
                    $"img:{normalized}",
                    WarmupJobKind.Image,
                    image.ImageUrl,
                    priority));
            }
        }
    }

    private void EnqueueWarmupJob(WarmupJob job)
    {
        if (!_queuedOrRunningWarmupKeys.TryAdd(job.Key, 0))
        {
            return;
        }

        lock (_warmupQueueLock)
        {
            _warmupQueue.Enqueue(job, job.Priority);
        }

        _warmupQueueSignal.Release();
    }

    private async Task<string?> EnsureImageCachedWithDedupeAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var normalized = imageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim().ToLowerInvariant();
        var task = _imageDownloadsInFlight.GetOrAdd(normalized, _ => DownloadImageToCacheCoreAsync(imageUrl));
        try
        {
            var cachedPath = await task;
            if (!string.IsNullOrWhiteSpace(cachedPath) && _pois != null)
            {
                ApplyCachedImagePaths(_pois);
            }

            return cachedPath;
        }
        finally
        {
            _imageDownloadsInFlight.TryRemove(normalized, out _);
        }
    }

    private async Task<string?> DownloadImageToCacheCoreAsync(string imageUrl)
    {
        if (File.Exists(imageUrl))
        {
            return imageUrl;
        }

        var cachedPath = GetImageCachePath(imageUrl);
        if (IsValidImageFile(cachedPath))
        {
            return cachedPath;
        }

        if (!IsRemoteImageCandidate(imageUrl))
        {
            return null;
        }

        foreach (var url in BuildImageUrlCandidates(imageUrl))
        {
            if (await TryDownloadImageToCacheAsync(url, cachedPath))
            {
                return cachedPath;
            }
        }

        return null;
    }

    private static bool IsRemoteImageCandidate(string imageUrl)
    {
        if (File.Exists(imageUrl))
        {
            return false;
        }

        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps;
        }

        var normalized = imageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim();
        if (normalized.StartsWith("Resources/Images/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.StartsWith("maui-images/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase);
    }

    private static void ApplyCachedImagePaths(IEnumerable<POI> pois)
    {
        foreach (var poi in pois)
        {
            if (poi.Images == null || poi.Images.Count == 0)
            {
                continue;
            }

            foreach (var image in poi.Images)
            {
                if (string.IsNullOrWhiteSpace(image.ImageUrl))
                {
                    continue;
                }

                if (File.Exists(image.ImageUrl))
                {
                    continue;
                }

                var cachedPath = GetImageCachePath(image.ImageUrl);
                if (IsValidImageFile(cachedPath))
                {
                    image.ImageUrl = cachedPath;
                }
            }
        }
    }

    private IEnumerable<string> BuildImageUrlCandidates(string imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return new[] { absoluteUri.ToString() };
        }

        var normalized = imageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim();
        var relative = normalized.TrimStart('/');

        var baseCandidates = new List<string>();
        if (_httpClient.BaseAddress != null)
        {
            baseCandidates.Add(_httpClient.BaseAddress.ToString());
        }

        baseCandidates.AddRange(AppSettings.ApiFallbackBaseUrls);

        return baseCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(baseUrl =>
            {
                try
                {
                    return new Uri(new Uri(baseUrl), relative).ToString();
                }
                catch
                {
                    return string.Empty;
                }
            })
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    private async Task<bool> TryDownloadImageToCacheAsync(string url, string cachePath)
    {
        var fileLock = GetFileWriteLock(cachePath);
        await fileLock.WaitAsync();
        try
        {
            if (IsValidImageFile(cachePath))
            {
                return true;
            }

            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var tempPath = $"{cachePath}.{Guid.NewGuid():N}.download";
            await using var source = await response.Content.ReadAsStreamAsync();
            await using (var output = File.Create(tempPath))
            {
                await source.CopyToAsync(output);
            }

            var size = new FileInfo(tempPath).Length;
            if (size < MinValidImageBytes)
            {
                File.Delete(tempPath);
                return false;
            }

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            File.Move(tempPath, cachePath);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    private SemaphoreSlim GetFileWriteLock(string path)
    {
        return _fileWriteLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
    }

    // Láº¥y táº¥t cáº£ cÃ¡c POIs Ä‘á»“ng bá»™
    public async Task<List<POI>> GetAllPOIsAsync()
    {
        var now = DateTime.UtcNow;
        var cacheAge = now - _lastFetchUtc;
        var hasMemory = _pois != null && _pois.Any();

        if (!hasMemory && _consecutiveFetchFailures > 0)
        {
            var failureAge = now - _lastFetchFailureUtc;
            if (_lastFetchFailureUtc != DateTime.MinValue && failureAge < FetchFailureCooldown)
            {
                Log($"[POIService][TTL] fetch-cooldown-active: failureAge={failureAge.TotalSeconds:F0}s < cooldown={FetchFailureCooldown.TotalSeconds:F0}s, failures={_consecutiveFetchFailures}");
                return new List<POI>();
            }
        }

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
            hasMemory = _pois != null && _pois.Any();

            if (!hasMemory && _consecutiveFetchFailures > 0)
            {
                var failureAge = now - _lastFetchFailureUtc;
                if (_lastFetchFailureUtc != DateTime.MinValue && failureAge < FetchFailureCooldown)
                {
                    Log($"[POIService][TTL] fetch-cooldown-active-after-lock: failureAge={failureAge.TotalSeconds:F0}s < cooldown={FetchFailureCooldown.TotalSeconds:F0}s, failures={_consecutiveFetchFailures}");
                    return new List<POI>();
                }
            }

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
    public Task<List<DishModel>> GetDishesByRestaurantIdAsync(string restaurantId)
    {
        if (string.IsNullOrWhiteSpace(restaurantId))
        {
            return Task.FromResult(new List<DishModel>());
        }

        return _dishRequestsInFlight.GetOrAdd(
            restaurantId,
            _ => LoadDishesByRestaurantIdCoreAsync(restaurantId));
    }

    private async Task<List<DishModel>> LoadDishesByRestaurantIdCoreAsync(string restaurantId)
    {
        try
        {
            var cachedDishes = await ReadDishesCacheAsync(restaurantId);

            var baseUrl = AppSettings.ApiBaseUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return cachedDishes;
            }

            var url = $"{baseUrl.TrimEnd('/')}/Restaurant/{restaurantId}/dishes";
            Log($"[POIService] Requesting dishes from: {url}");
            var dishes = await _httpClient.GetFromJsonAsync<List<DishModel>>(url);

            if (dishes != null)
            {
                foreach (var dish in dishes)
                {
                    Log($"[POIService] Dish: {dish.Name}, ImageFileName: {dish.ImageFileName}");
                }

                await SaveDishesCacheAsync(restaurantId, dishes);
            }

            return dishes ?? new List<DishModel>();
        }
        catch (Exception ex)
        {
            Log($"[POIService] GetDishes failed: {ex.Message}");
            return await ReadDishesCacheAsync(restaurantId);
        }
        finally
        {
            _dishRequestsInFlight.TryRemove(restaurantId, out _);
        }
    }
}

