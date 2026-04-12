using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Diagnostics;



namespace food_market_narrator.Services;

// Service quản lý POI: cache-first load, refresh network, warmup ảnh/món ăn và geofence state.
public class POIService : IPOIService
{
    private enum WarmupJobKind
    {
        Image = 0,
        Dishes = 1
    }

    // Đơn vị công việc warm-up cho queue nền (ảnh hoặc món ăn).
    private sealed record WarmupJob(string Key, WarmupJobKind Kind, string Value, int Priority);

    // Trạng thái geofence/POI gần nhất đang theo dõi trong phiên hiện tại.
    private POI? _lastNearest;
    private bool _isInsidePOI = false;

    // Cache POI trong memory và các mốc thời gian phục vụ TTL/cooldown refresh.
    private List<POI>? _pois;
    private DateTime _lastFetchUtc = DateTime.MinValue;
    private DateTime _lastFetchFailureUtc = DateTime.MinValue;
    private int _consecutiveFetchFailures;
    private bool _lastLoadSucceededFromNetwork;

    // Cấu hình refresh POI từ network.
    private static readonly TimeSpan PoiTtl = TimeSpan.FromMinutes(3);
    private static readonly TimeSpan FetchFailureCooldown = TimeSpan.FromSeconds(20);
    private static readonly TimeSpan RestaurantRequestTimeout = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan DishesRequestTimeout = TimeSpan.FromSeconds(3);

    // Lock điều phối refresh để tránh nhiều request/network warmup chạy chồng nhau.
    private readonly SemaphoreSlim _networkRefreshLock = new(1, 1);
    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private readonly SemaphoreSlim _offlineWarmupLock = new(1, 1);
    private Task? _offlineWarmupTask;

    // Queue warm-up nền và tín hiệu worker xử lý theo độ ưu tiên.
    private readonly object _warmupQueueLock = new();
    private readonly PriorityQueue<WarmupJob, int> _warmupQueue = new();
    private readonly SemaphoreSlim _warmupQueueSignal = new(0);

    // Dedupe in-flight để tránh enqueue/request/download trùng lặp.
    private readonly ConcurrentDictionary<string, byte> _queuedOrRunningWarmupKeys = new(StringComparer.OrdinalIgnoreCase); // tránh enqueue job trùng lặp dựa trên job.Key
    private readonly ConcurrentDictionary<string, Task<string?>> _imageDownloadsInFlight = new(StringComparer.OrdinalIgnoreCase); // tránh request/download ảnh trùng lặp dựa trên URL nguồn
    private readonly ConcurrentDictionary<string, Task<List<DishModel>>> _dishRequestsInFlight = new(StringComparer.OrdinalIgnoreCase); // tránh request món ăn trùng lặp dựa trên restaurantId

    // Khóa ghi file cache và limiter cho số tác vụ warm-up chạy song song.
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _fileWriteLocks = new(StringComparer.OrdinalIgnoreCase); // tránh ghi file trùng lặp dựa trên key
    private readonly SemaphoreSlim _imageWarmupLimiter = new(AppSettings.OfflineWarmupImageConcurrency, AppSettings.OfflineWarmupImageConcurrency); // giới hạn số tác vụ warm-up ảnh chạy đồng thời để tránh quá tải mạng và I/O
    private readonly SemaphoreSlim _dishWarmupLimiter = new(1, 1); // giới hạn số tác vụ warm-up món ăn chạy đồng thời
    private int _warmupWorkersStarted;

    // Hằng số cache/warm-up dùng chung trong toàn bộ lifecycle của service.
    private const string ImageCacheFolderName = "image_cache"; // tên thư mục lưu cache ảnh đã tải về, nằm trong thư mục app data của ứng dụng
    private const int MinValidImageBytes = 128; // kích thước tối thiểu của một file ảnh hợp lệ sau khi tải về, dùng để xác định xem ảnh đã tải có thành công và có nội dung hay không (tránh cache các file lỗi hoặc trống)
    private const int WarmupWorkerCount = 2; // số lượng worker nền xử lý queue warm-up, có thể điều chỉnh để cân bằng giữa tốc độ warm-up và tài nguyên hệ thống (CPU, mạng, I/O)
    private const int WarmupPhaseATopCount = 6; // số lượng POI ưu tiên trong phase A của warm-up, thường là các POI có nhiều audio hoặc lượt truy cập để đảm bảo trải nghiệm mượt mà cho phần lớn người dùng ngay sau khi load POI từ cache hoặc network. Các POI còn lại sẽ được xếp vào phase B để warm-up sau đó nhằm tối ưu tài nguyên và tránh quá tải khi mới load POI.
    private const int WarmupPriorityHigh = 0; // độ ưu tiên cao cho các job warm-up ảnh của POI ưu tiên trong phase A, giúp đảm bảo các POI này có trải nghiệm tốt nhất ngay sau khi load
    private const int WarmupPriorityNormal = 1; // độ ưu tiên bình thường cho các job warm-up còn lại, sẽ được xử lý sau khi các job ưu tiên đã được xếp hàng và xử lý
    private static readonly TimeSpan WarmupInitialDelay = TimeSpan.FromMilliseconds(AppSettings.OfflineWarmupInitialDelayMs);
    private static readonly TimeSpan WarmupPhaseBDelay = TimeSpan.FromMilliseconds(AppSettings.OfflineWarmupPhaseBDelayMs);
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    // HttpClient dùng cho các request lấy POI, ảnh và món ăn.
    private readonly HttpClient _httpClient;

    public POIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        Log($"[POIService] HttpClient.BaseAddress = {_httpClient.BaseAddress}");
    }

    // Ghi log debug/runtime, có lọc bớt log ảnh không quan trọng theo cấu hình.
    private static void Log(string message)
    {
        if (ShouldSkipVerboseImageLog(message))
        {
            return;
        }

        Debug.WriteLine(message);
        Console.WriteLine(message);
    }

    // Lọc bớt các log liên quan đến warm-up ảnh nếu cấu hình verbose mode không bật, chỉ giữ lại các log lỗi hoặc thông tin quan trọng để tránh quá nhiều log khi warm-up ảnh hàng loạt.
    private static bool ShouldSkipVerboseImageLog(string message)
    {
        if (AppSettings.EnableVerboseImageWarmupLogs)
        {
            return false;
        }

        if (!message.Contains("[POIService][Image]", StringComparison.Ordinal))
        {
            return false;
        }

        // Keep only actionable image logs when verbose mode is disabled.
        return !message.Contains("http-failed", StringComparison.Ordinal)
            && !message.Contains("download-exception", StringComparison.Ordinal)
            && !message.Contains("all-candidates-failed", StringComparison.Ordinal)
            && !message.Contains("download-flow-failed", StringComparison.Ordinal)
            && !message.Contains("file-too-small", StringComparison.Ordinal)
            && !message.Contains("skip-non-remote-candidate", StringComparison.Ordinal)
            && !message.Contains("401", StringComparison.Ordinal)
            && !message.Contains("403", StringComparison.Ordinal)
            && !message.Contains("404", StringComparison.Ordinal);
    }

    // Chuẩn hóa thông tin exception để log ngắn gọn nhưng vẫn có inner exception khi cần.
    private static string FormatException(Exception ex)
    {
        var inner = ex.InnerException?.Message;
        return $"{ex.GetType().Name}: {ex.Message}" + (string.IsNullOrWhiteSpace(inner) ? string.Empty : $" | Inner: {inner}");
    }

    // Lấy POI theo chiến lược cache-first: memory -> disk -> network.
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

        if (cachedPois.Count > 0)
        {
            _pois = cachedPois
                .Where(p => p.IsActive)
                .ToList();
            ApplyCachedImagePaths(_pois);
            StartOfflineAssetWarmup(_pois);
            _lastLoadSucceededFromNetwork = false;
            _lastFetchUtc = DateTime.UtcNow;

            var totalAudios = _pois.Sum(p => p.Audios?.Count ?? 0);
            Log($"[POIService] Loaded {_pois.Count} POIs and {totalAudios} audios from offline cache (fast path). | elapsedMs={swTotal.ElapsedMilliseconds}");

            if (Connectivity.Current.NetworkAccess == NetworkAccess.Internet)
            {
                _ = TryRefreshPoisFromNetworkAsync(runInBackground: true);
            }

            return _pois;
        }

        if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
        {
            _lastLoadSucceededFromNetwork = false;
            return new List<POI>();
        }

        return await TryRefreshPoisFromNetworkAsync(runInBackground: false);
    }

    // Thử refresh POI từ network qua danh sách endpoint fallback. để lấy POI mới nhất và đảm bảo trải nghiệm tốt nhất cho người dùng, đồng thời cập nhật cache và warm-up nền. Nếu runInBackground=true thì sẽ không chờ kết quả network mà trả về ngay cache hiện tại (nếu có) và refresh POI trong nền, ngược lại sẽ chờ kết quả network để trả về POI mới nhất.
    private async Task<List<POI>> TryRefreshPoisFromNetworkAsync(bool runInBackground)
    {
        if (runInBackground)
        {
            if (!await _networkRefreshLock.WaitAsync(0))
            {
                return _pois ?? new List<POI>();
            }
        }
        else
        {
            await _networkRefreshLock.WaitAsync();
        }

        try
        {
            var swTotal = Stopwatch.StartNew();
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
                    _lastFetchUtc = DateTime.UtcNow;
                    var totalAudios = _pois.Sum(p => p.Audios?.Count ?? 0);
                    Log($"[POIService] Loaded {_pois.Count} POIs and {totalAudios} audios from {requestUrl} | elapsedMs={sw.ElapsedMilliseconds} | totalElapsedMs={swTotal.ElapsedMilliseconds}");
                    return _pois;
                }
                catch (Exception ex)
                {
                    Log($"[POIService] Request failed: {baseUrl} -> {FormatException(ex)}");
                }
            }

            _lastLoadSucceededFromNetwork = false;
            _consecutiveFetchFailures++;
            _lastFetchFailureUtc = DateTime.UtcNow;
            Log($"[POIService] Error fetching POIs from all candidates. | failures={_consecutiveFetchFailures} | elapsedMs={swTotal.ElapsedMilliseconds}");

            return _pois ?? new List<POI>();
        }
        finally
        {
            _networkRefreshLock.Release();
        }
    }

    // Trả về đường dẫn file cache pois.json trong bộ nhớ app (và đảm bảo thư mục tồn tại)
    private static string GetPoiCacheFilePath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "offline_cache");
        Directory.CreateDirectory(cacheDir);
        return Path.Combine(cacheDir, "pois.json");
    }

    // Đọc cache POI từ file, nếu có. Nếu đọc thành công sẽ trả về danh sách POI, ngược lại sẽ trả về danh sách rỗng. Hàm này được gọi khi load POI lần đầu tiên để có dữ liệu hiển thị nhanh, sau đó sẽ refresh từ network nếu có kết nối.
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

    // Lưu danh sách POI vào file cache để dùng cho lần sau khi không thể gọi API. Hàm này được gọi sau khi load POI thành công từ network để cập nhật cache và làm nguồn dữ liệu nhanh cho lần load tiếp theo.
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

    
    // Lấy (và đảm bảo tồn tại) thư mục gốc để lưu cache offline như POI và món ăn, tránh lỗi khi ghi file cache vào thư mục không tồn tại. Hàm này được gọi trước khi đọc hoặc ghi cache để đảm bảo thư mục đã sẵn sàng.
    private static string GetOfflineCacheRootPath()
    {
        var cacheDir = Path.Combine(FileSystem.AppDataDirectory, "offline_cache");
        Directory.CreateDirectory(cacheDir);
        return cacheDir;
    }

    // Tương tự như GetPoiCacheFilePath nhưng dành cho cache món ăn theo restaurantId, đảm bảo tên file hợp lệ và thư mục tồn tại. Hàm này được gọi khi đọc hoặc ghi cache món ăn để xác định đường dẫn file cache tương ứng với restaurantId.
    private static string GetDishesCacheFilePath(string restaurantId)
    {
        var dishesDir = Path.Combine(GetOfflineCacheRootPath(), "dishes");
        Directory.CreateDirectory(dishesDir);
        var safeId = string.Join("_", restaurantId.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        return Path.Combine(dishesDir, $"{safeId}.json");
    }

    // Đọc cache món ăn theo restaurantId, trả list rỗng nếu file không có hoặc lỗi parse.
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

    // Ghi cache món ăn theo restaurantId, dùng file lock để tránh ghi chồng nhiều luồng.
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

    // Lấy thư mục gốc cache ảnh và tự tạo nếu chưa tồn tại.
    private static string GetImageCacheRootPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, ImageCacheFolderName);
        Directory.CreateDirectory(path);
        return path;
    }

    // Tạo đường dẫn cache ảnh theo hash từ nguồn ảnh để dedupe ổn định.
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

    // Kiểm tra file ảnh cache có tồn tại và đủ kích thước tối thiểu.
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

    // Bắt đầu warm-up nền cho ảnh và món ăn theo 2 phase ưu tiên.
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
                // Delay warm-up a bit so first render and first interactions stay smooth.
                await Task.Delay(WarmupInitialDelay);
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

    // Khởi động worker xử lý queue warm-up một lần duy nhất trong lifecycle service.
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

    // Vòng lặp worker: lấy job từ queue và xử lý theo loại ảnh/món ăn.
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

    // Enqueue warm-up cho mỗi POI: dishes theo restaurantId và ảnh theo rule ưu tiên.
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
                Log($"[POIService][Image] skip-poi-no-images: poi={poi.restaurantId}");
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

            Log($"[POIService][Image] warmup-candidates: poi={poi.restaurantId}, count={imageCandidates.Count}, priority={priority}, includeAll={includeAllImages}");

            foreach (var image in imageCandidates)
            {
                if (string.IsNullOrWhiteSpace(image.ImageUrl))
                {
                    Log($"[POIService][Image] skip-empty-image-url: poi={poi.restaurantId}");
                    continue;
                }

                if (File.Exists(image.ImageUrl))
                {
                    Log($"[POIService][Image] skip-local-file-exists: poi={poi.restaurantId}, image={image.ImageUrl}");
                    continue;
                }

                var cachedPath = GetImageCachePath(image.ImageUrl);
                if (IsValidImageFile(cachedPath))
                {
                    Log($"[POIService][Image] skip-already-cached: poi={poi.restaurantId}, source={image.ImageUrl}, cache={cachedPath}");
                    continue;
                }

                if (!IsRemoteImageCandidate(image.ImageUrl))
                {
                    Log($"[POIService][Image] skip-non-remote-candidate: poi={poi.restaurantId}, image={image.ImageUrl}");
                    continue;
                }

                var normalized = image.ImageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim().ToLowerInvariant();
                Log($"[POIService][Image] enqueue-download: poi={poi.restaurantId}, source={image.ImageUrl}");
                EnqueueWarmupJob(new WarmupJob(
                    $"img:{normalized}",
                    WarmupJobKind.Image,
                    image.ImageUrl,
                    priority));
            }
        }
    }

    // Enqueue một job warm-up có dedupe theo key để tránh duplicate queue.
    private void EnqueueWarmupJob(WarmupJob job)
    {
        if (!_queuedOrRunningWarmupKeys.TryAdd(job.Key, 0))
        {
            if (job.Kind == WarmupJobKind.Image)
            {
                Log($"[POIService][Image] dedupe-queue-hit: key={job.Key}");
            }
            return;
        }

        lock (_warmupQueueLock)
        {
            _warmupQueue.Enqueue(job, job.Priority);
        }

        _warmupQueueSignal.Release();
    }

    // Đảm bảo 1 nguồn ảnh chỉ có một flow download in-flight tại cùng thời điểm.
    private async Task<string?> EnsureImageCachedWithDedupeAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var normalized = imageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim().ToLowerInvariant();
        var hadInFlight = _imageDownloadsInFlight.ContainsKey(normalized);
        var task = _imageDownloadsInFlight.GetOrAdd(normalized, _ => DownloadImageToCacheCoreAsync(imageUrl));
        if (hadInFlight)
        {
            Log($"[POIService][Image] dedupe-inflight-hit: source={imageUrl}");
        }
        else
        {
            Log($"[POIService][Image] start-download-flow: source={imageUrl}");
        }

        try
        {
            var cachedPath = await task;
            if (!string.IsNullOrWhiteSpace(cachedPath) && _pois != null)
            {
                ApplyCachedImagePaths(_pois);
            }

            Log(string.IsNullOrWhiteSpace(cachedPath)
                ? $"[POIService][Image] download-flow-failed: source={imageUrl}"
                : $"[POIService][Image] download-flow-success: source={imageUrl}, cache={cachedPath}");

            return cachedPath;
        }
        finally
        {
            _imageDownloadsInFlight.TryRemove(normalized, out _);
        }
    }

    // Core download ảnh: thử qua danh sách URL candidate và ghi vào cache khi thành công.
    private async Task<string?> DownloadImageToCacheCoreAsync(string imageUrl)
    {
        if (File.Exists(imageUrl))
        {
            Log($"[POIService][Image] download-skip-local-exists: source={imageUrl}");
            return imageUrl;
        }

        var cachedPath = GetImageCachePath(imageUrl);
        if (IsValidImageFile(cachedPath))
        {
            Log($"[POIService][Image] download-skip-cache-hit: source={imageUrl}, cache={cachedPath}");
            return cachedPath;
        }

        if (!IsRemoteImageCandidate(imageUrl))
        {
            Log($"[POIService][Image] download-skip-non-remote: source={imageUrl}");
            return null;
        }

        var candidates = BuildImageUrlCandidates(imageUrl).ToList();
        Log($"[POIService][Image] url-candidates: source={imageUrl}, count={candidates.Count}, candidates={string.Join(" | ", candidates)}");

        foreach (var url in candidates)
        {
            if (await TryDownloadImageToCacheAsync(url, cachedPath))
            {
                Log($"[POIService][Image] download-success: source={imageUrl}, url={url}, cache={cachedPath}");
                return cachedPath;
            }

            Log($"[POIService][Image] download-attempt-failed: source={imageUrl}, url={url}");
        }

        Log($"[POIService][Image] all-candidates-failed: source={imageUrl}");
        return null;
    }

    // Xác định imageUrl có phải nguồn remote hợp lệ để đưa vào warm-up/download hay không.
    private static bool IsRemoteImageCandidate(string imageUrl)
    {
        if (File.Exists(imageUrl))
        {
            return false;
        }

        var normalized = imageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim();
        if (normalized.StartsWith("Resources/Images/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // IMPORTANT: check app-relative/static paths before Uri.TryCreate,
        // because strings like "/maui-images/a.jpg" may be parsed as absolute file URIs.
        if (normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.StartsWith("maui-images/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("uploads/images/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps;
        }

        // Accept bare file names like "foo.jpg" from legacy image data.
        if (!normalized.Contains('/', StringComparison.Ordinal) && HasImageLikeExtension(normalized))
        {
            return true;
        }

        return false;
    }

    // Kiểm tra extension có thuộc nhóm định dạng ảnh hỗ trợ.
    private static bool HasImageLikeExtension(string path)
    {
        var ext = Path.GetExtension(path);
        if (string.IsNullOrWhiteSpace(ext))
        {
            return false;
        }

        return ext.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".jpeg", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".webp", StringComparison.OrdinalIgnoreCase)
            || ext.Equals(".gif", StringComparison.OrdinalIgnoreCase);
    }

    // Áp dụng đường dẫn ảnh cache local cho POI nếu file đã được warm-up trước đó.
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

    // Xây URL ứng viên để tải ảnh từ base URL hiện tại và các fallback URL.
    private IEnumerable<string> BuildImageUrlCandidates(string imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return new[] { absoluteUri.ToString() };
        }

        var normalized = imageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim();
        var relatives = new List<string>();

        if (normalized.StartsWith("/", StringComparison.Ordinal))
        {
            relatives.Add(normalized.TrimStart('/'));
        }
        else if (normalized.StartsWith("maui-images/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
        {
            relatives.Add(normalized);
        }
        else if (!normalized.Contains('/', StringComparison.Ordinal) && HasImageLikeExtension(normalized))
        {
            // Legacy DB rows often store just file name; default them to maui-images static path.
            relatives.Add($"maui-images/{normalized}");
            relatives.Add(normalized);
        }
        else
        {
            relatives.Add(normalized.TrimStart('/'));
        }

        var baseCandidates = new List<string>();
        if (_httpClient.BaseAddress != null)
        {
            baseCandidates.Add(_httpClient.BaseAddress.ToString());
        }

        baseCandidates.AddRange(AppSettings.ApiFallbackBaseUrls);

        return baseCandidates
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .SelectMany(baseUrl => relatives.Select(relative =>
            {
                try
                {
                    return new Uri(new Uri(baseUrl), relative).ToString();
                }
                catch
                {
                    return string.Empty;
                }
            }))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    // Tải một ảnh về cache bằng khóa ghi file theo path để tránh race-condition.
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
                Log($"[POIService][Image] http-failed: url={url}, status={(int)response.StatusCode} {response.StatusCode}");
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
                Log($"[POIService][Image] file-too-small: url={url}, bytes={size}, min={MinValidImageBytes}");
                File.Delete(tempPath);
                return false;
            }

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            File.Move(tempPath, cachePath);
            Log($"[POIService][Image] cache-write-success: url={url}, cache={cachePath}, bytes={size}");
            return true;
        }
        catch (Exception ex)
        {
            Log($"[POIService][Image] download-exception: url={url}, error={FormatException(ex)}");
            return false;
        }
        finally
        {
            fileLock.Release();
        }
    }

    // Lấy lock theo từng file cache để serialize thao tác ghi/xóa/đổi tên file.
    private SemaphoreSlim GetFileWriteLock(string path)
    {
        return _fileWriteLocks.GetOrAdd(path, _ => new SemaphoreSlim(1, 1));
    }

    // Lấy toàn bộ POI có xét TTL và cooldown khi fetch lỗi liên tiếp.
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

    // Tìm POI theo restaurantId từ tập dữ liệu POI đã refresh theo TTL hiện tại.
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

    // Lấy POI gần nhất theo tọa độ lat/lng hiện tại.
    public POI? GetNearestPOI(double currentLat, double currentLng)
    {
        return GetNearestPOI(new Location(currentLat, currentLng), _pois);
    }

    // Lấy POI gần nhất từ tập dữ liệu đầu vào hoặc cache _pois hiện tại.
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

    // Tính khoảng cách mét giữa vị trí hiện tại và tâm của một POI.
    public double GetDistanceMeters(Location currentLocation, POI poi)
    {
        return Location.CalculateDistance(
            currentLocation,
            new Location(poi.Latitude, poi.Longitude),
            DistanceUnits.Kilometers) * 1000;
    }

    // Cập nhật geofence state và chỉ trả POI khi xảy ra transition enter/switch.
    public POI? UpdateNearestPOI(double currentLat, double currentLng, IEnumerable<POI>? pois = null)
    {
        var source = pois?.ToList() ?? _pois;
        if (source == null || source.Count == 0)
            return null;

        var currentLocation = new Location(currentLat, currentLng);

        var nearest = GetNearestPOI(currentLocation, source);

        if (nearest == null)
            return null;

        var minDistance = GetDistanceMeters(currentLocation, nearest);

        if (!_isInsidePOI)
        {
            // Chưa ở trong POI: xét điều kiện vào vùng theo EnterRadius.
            if (minDistance <= AppSettings.PoiEnterRadiusMeters)
            {
                _isInsidePOI = true;
                _lastNearest = nearest;

                return nearest; // Trigger khi mới vào.
            }
        }
        else
        {
            // Đang ở trong POI.
            // Nếu đổi sang POI khác và vẫn đủ gần thì trigger POI mới.
            if (nearest != _lastNearest && minDistance <= AppSettings.PoiEnterRadiusMeters)
            {
                _lastNearest = nearest;
                return nearest; // Trigger POI mới.
            }

            // Nếu đi xa khỏi POI hiện tại hơn ExitRadius thì thoát trạng thái inside.
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

        return null; // Không có transition geofence
    }

    // Reset trạng thái geofence khi bắt đầu phiên narration mới hoặc cần clear state.
    public void ResetGeofenceState()
    {
        _isInsidePOI = false;
        _lastNearest = null;
    }

    // Lấy danh sách món theo restaurant, có dedupe request in-flight.
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

    // Load dishes từ network và fallback cache nếu lỗi, đồng thời cập nhật cache khi thành công.
    private async Task<List<DishModel>> LoadDishesByRestaurantIdCoreAsync(string restaurantId)
    {
        try
        {
            var cachedDishes = await ReadDishesCacheAsync(restaurantId);

            if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
            {
                return cachedDishes;
            }

            var baseCandidates = new List<string>();

            if (_httpClient.BaseAddress != null)
            {
                baseCandidates.Add(_httpClient.BaseAddress.ToString());
            }

            baseCandidates.Add(AppSettings.ApiBaseUrl);
            baseCandidates.AddRange(AppSettings.ApiFallbackBaseUrls);

            var uniqueBaseCandidates = baseCandidates
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var baseUrl in uniqueBaseCandidates)
            {
                try
                {
                    var requestUrl = new Uri(new Uri(baseUrl), $"Restaurant/{restaurantId}/dishes");
                    using var cts = new CancellationTokenSource(DishesRequestTimeout);
                    Log($"[POIService] Requesting dishes from: {requestUrl} | timeout={DishesRequestTimeout.TotalSeconds:F0}s");

                    var dishes = await _httpClient.GetFromJsonAsync<List<DishModel>>(requestUrl, cts.Token);
                    if (dishes == null)
                    {
                        continue;
                    }

                    foreach (var dish in dishes)
                    {
                        Log($"[POIService] Dish: {dish.Name}, ImageFileName: {dish.ImageFileName}");
                    }

                    await SaveDishesCacheAsync(restaurantId, dishes);
                    return dishes;
                }
                catch (Exception ex)
                {
                    Log($"[POIService] GetDishes failed: {baseUrl} -> {FormatException(ex)}");
                }
            }

            return cachedDishes;
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

