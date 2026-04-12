using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text.Json;

namespace food_market_narrator.Services;

/// <summary>
/// Background warmup service for tour stop images.
/// Uses the same image_cache folder as POIService so cached images are shared.
/// </summary>
public class TourImageWarmupService
{
    private const string ImageCacheFolderName = "image_cache";
    private const int MinValidImageBytes = 128;
    private const int WarmupPhaseATopCount = 6;
    private static readonly TimeSpan WarmupInitialDelay = TimeSpan.FromMilliseconds(AppSettings.OfflineWarmupInitialDelayMs);
    private static readonly TimeSpan WarmupPhaseBDelay = TimeSpan.FromMilliseconds(AppSettings.OfflineWarmupPhaseBDelayMs);

    private readonly HttpClient _httpClient;
    private readonly ConcurrentDictionary<string, byte> _queuedOrRunningKeys = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, Task<string?>> _downloadsInFlight = new(StringComparer.OrdinalIgnoreCase);
    private readonly SemaphoreSlim _downloadLimiter = new(1, 1);
    private Task? _warmupTask;

    // Khởi tạo service warm-up ảnh tour với HttpClient dùng chung của app.
    public TourImageWarmupService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Kicks off background image warmup for the given tours.
    /// Safe to call multiple times; concurrent calls are deduplicated.
    /// </summary>
    public void WarmupTourImages(List<TourModel> tours)
    {
        if (tours == null || tours.Count == 0)
        {
            return;
        }

        var phaseAImages = new List<string>();
        var phaseBImages = new List<string>();

        foreach (var tour in tours)
        {
            if (!string.IsNullOrWhiteSpace(tour.ImageUrl) && IsRemoteImageCandidate(tour.ImageUrl))
            {
                phaseAImages.Add(tour.ImageUrl);
            }

            if (tour.Stops == null)
            {
                continue;
            }

            foreach (var stop in tour.Stops)
            {
                if (string.IsNullOrWhiteSpace(stop.PrimaryImageUrl))
                {
                    continue;
                }

                if (!IsRemoteImageCandidate(stop.PrimaryImageUrl))
                {
                    continue;
                }

                if (phaseAImages.Count < WarmupPhaseATopCount)
                {
                    phaseAImages.Add(stop.PrimaryImageUrl);
                }
                else
                {
                    phaseBImages.Add(stop.PrimaryImageUrl);
                }
            }
        }

        phaseAImages = phaseAImages
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        phaseBImages = phaseBImages
            .Except(phaseAImages, StringComparer.OrdinalIgnoreCase)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (phaseAImages.Count == 0 && phaseBImages.Count == 0)
        {
            return;
        }

        if (_warmupTask != null && !_warmupTask.IsCompleted)
        {
            return;
        }

        _warmupTask = Task.Run(() => RunWarmupAsync(phaseAImages, phaseBImages));
    }

    // Chạy warm-up theo 2 pha: pha A tải sớm ảnh ưu tiên, pha B tải phần còn lại sau delay.
    private async Task RunWarmupAsync(List<string> phaseAImages, List<string> phaseBImages)
    {
        try
        {
            await Task.Delay(WarmupInitialDelay);

            // Phase A — top N images immediately
            foreach (var imageUrl in phaseAImages)
            {
                await EnsureImageCachedAsync(imageUrl);
            }

            // Phase B — remaining images after delay
            if (phaseBImages.Count > 0)
            {
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(WarmupPhaseBDelay);
                        foreach (var imageUrl in phaseBImages)
                        {
                            await EnsureImageCachedAsync(imageUrl);
                        }
                    }
                    catch
                    {
                        // Warmup failures are non-critical
                    }
                });
            }
        }
        catch
        {
            // Warmup failures are non-critical
        }
    }

    // Đảm bảo một nguồn ảnh chỉ có một luồng warm-up active tại một thời điểm.
    private async Task<string?> EnsureImageCachedAsync(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return null;
        }

        var normalized = NormalizeImageUrl(imageUrl);

        // Deduplicate: skip if already queued or in-flight
        if (!_queuedOrRunningKeys.TryAdd(normalized, 0))
        {
            return null;
        }

        try
        {
            // Check if already cached on disk
            var cachedPath = GetImageCachePath(imageUrl);
            if (IsValidImageFile(cachedPath))
            {
                return cachedPath;
            }

            // Try to download
            var hadInFlight = _downloadsInFlight.ContainsKey(normalized);
            var task = _downloadsInFlight.GetOrAdd(normalized, _ => DownloadImageToCacheAsync(imageUrl, cachedPath));
            if (hadInFlight)
            {
                return await task;
            }

            var result = await task;
            return result;
        }
        finally
        {
            _downloadsInFlight.TryRemove(normalized, out _);
            _queuedOrRunningKeys.TryRemove(normalized, out _);
        }
    }

    // Tải ảnh vào cache local bằng danh sách URL ứng viên và trả path cache nếu thành công.
    private async Task<string?> DownloadImageToCacheAsync(string imageUrl, string cachePath)
    {
        // Skip if file already exists and is valid
        if (IsValidImageFile(cachePath))
        {
            return cachePath;
        }

        // Build URL candidates
        var candidates = BuildImageUrlCandidates(imageUrl).ToList();
        if (candidates.Count == 0)
        {
            return null;
        }

        await _downloadLimiter.WaitAsync();
        try
        {
            // Double-check after acquiring semaphore
            if (IsValidImageFile(cachePath))
            {
                return cachePath;
            }

            foreach (var url in candidates)
            {
                try
                {
                    using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
                    if (!response.IsSuccessStatusCode)
                    {
                        continue;
                    }

                    var tempPath = $"{cachePath}.{Guid.NewGuid():N}.download";
                    await using var source = await response.Content.ReadAsStreamAsync();
                    await using var output = File.Create(tempPath);
                    await source.CopyToAsync(output);

                    var size = new FileInfo(tempPath).Length;
                    if (size < MinValidImageBytes)
                    {
                        File.Delete(tempPath);
                        continue;
                    }

                    if (File.Exists(cachePath))
                    {
                        File.Delete(cachePath);
                    }

                    File.Move(tempPath, cachePath);
                    return cachePath;
                }
                catch
                {
                    // Try next candidate
                }
            }
        }
        finally
        {
            _downloadLimiter.Release();
        }

        return null;
    }

    // Chuẩn hóa chuỗi URL ảnh để so sánh dedupe ổn định.
    private static string NormalizeImageUrl(string imageUrl)
    {
        return imageUrl.Replace("\\", "/", StringComparison.Ordinal).Trim().ToLowerInvariant();
    }

    // Sinh path cache ảnh từ hash URL nguồn để tránh trùng tên file.
    private static string GetImageCachePath(string source)
    {
        var normalized = NormalizeImageUrl(source);
        var ext = Path.GetExtension(normalized);
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = ".img";
        }

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(normalized)));
        return Path.Combine(GetImageCacheRootPath(), $"{hash}{ext}");
    }

    // Lấy thư mục cache ảnh dùng chung và tạo mới nếu chưa tồn tại.
    private static string GetImageCacheRootPath()
    {
        var path = Path.Combine(FileSystem.AppDataDirectory, ImageCacheFolderName);
        Directory.CreateDirectory(path);
        return path;
    }

    // Kiểm tra file cache ảnh có hợp lệ theo ngưỡng dung lượng tối thiểu.
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

    // Xác định ảnh có phải nguồn remote hợp lệ để đưa vào warm-up.
    private static bool IsRemoteImageCandidate(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return false;
        }

        if (File.Exists(imageUrl))
        {
            return false;
        }

        var normalized = NormalizeImageUrl(imageUrl);

        // Skip embedded resources
        if (normalized.StartsWith("resources/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        // Accept absolute HTTP(S) URLs
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absoluteUri))
        {
            return absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps;
        }

        // Accept relative paths starting with slash or known static prefixes
        if (normalized.StartsWith("/", StringComparison.Ordinal)
            || normalized.StartsWith("maui-images/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("uploads/images/", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Accept bare filenames with image-like extension
        if (!normalized.Contains('/', StringComparison.Ordinal) && HasImageLikeExtension(normalized))
        {
            return true;
        }

        return false;
    }

    // Kiểm tra extension có thuộc nhóm định dạng ảnh hỗ trợ hay không.
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

    // Dựng danh sách URL ứng viên từ base URL hiện tại và các fallback base URL.
    private IEnumerable<string> BuildImageUrlCandidates(string imageUrl)
    {
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var absoluteUri)
            && (absoluteUri.Scheme == Uri.UriSchemeHttp || absoluteUri.Scheme == Uri.UriSchemeHttps))
        {
            return new[] { absoluteUri.ToString() };
        }

        var normalized = NormalizeImageUrl(imageUrl);
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
            .SelectMany(baseUrl =>
            {
                var trimmed = baseUrl.TrimEnd('/');
                return relatives.Select(relative =>
                {
                    try
                    {
                        return new Uri(new Uri(trimmed), relative).ToString();
                    }
                    catch
                    {
                        return string.Empty;
                    }
                });
            })
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }
}
