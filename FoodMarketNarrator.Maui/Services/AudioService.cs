using Plugin.Maui.Audio;
using food_market_narrator.Settings;
using System.Security.Cryptography;
using System.Diagnostics;

namespace food_market_narrator.Services;

// Class này quản lý toàn bộ vòng đời phát audio trong app:
// tìm nguồn phát (cache/package/remote), cache theo LRU + quota, và xử lý audio focus theo nền tảng.
public partial class AudioService : IAudioService
{
    private readonly IAudioManager _audioManager;
    private readonly HttpClient _httpClient;
    private IAudioPlayer? _player;
    private bool _isPaused;
    private string? _currentTrackKey;
    private const int MinValidAudioBytes = 256;
    private const long MaxAudioCacheBytes = 200L * 1024 * 1024;
    private const long MinDeviceFreeSpaceBytes = 50L * 1024 * 1024;
    private const string AudioCacheFolderName = "audio_cache";
    public bool IsPlaying => _player?.IsPlaying ?? false;
    public bool IsPaused => _isPaused;
    public string? CurrentTrackKey => _currentTrackKey;
    public TimeSpan Duration => TimeSpan.FromSeconds(_player?.Duration ?? 0d);
    public TimeSpan CurrentPosition => TimeSpan.FromSeconds(_player?.CurrentPosition ?? 0d);
    public event EventHandler? PlaybackEnded;

    public AudioService(HttpClient httpClient)
    {
        _audioManager = AudioManager.Current;
        _httpClient = httpClient;
        InitializePlatformInterruptionHandling();
    }

    partial void InitializePlatformInterruptionHandling();
    partial void RequestPlatformAudioFocus();
    partial void ReleasePlatformAudioFocus();


    // ================ Audio Methods ================


    // Phát audio theo ngôn ngữ + tên file, ưu tiên local cache rồi mới fallback sang package/remote.
    public async Task PlaySound(string language, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            // Console.WriteLine("File name null -> skip");
            return;
        }

        StopSound();
        _isPaused = false;

        try
        {
            _currentTrackKey = ResolveAudioPath(language, fileName);
            // Console.WriteLine($"Loading audio key: {_currentTrackKey}");

            await using var stream = await ResolvePlayableStreamAsync(language, fileName);
            if (stream == null)
            {
                // Console.WriteLine($"Audio not found for input: {fileName}");
                _currentTrackKey = null;
                return;
            }

            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _player = _audioManager.CreatePlayer(memoryStream);
            _player.PlaybackEnded += OnPlaybackEnded;
            RequestPlatformAudioFocus();
            _player.Play();

            // Console.WriteLine("Audio started");
        }
        catch (Exception)
        {
            // Console.WriteLine($"ERROR PLAY SOUND: {ex}");
            _currentTrackKey = null;
        }
    }

    // Phát audio theo audioId, phù hợp với luồng lấy file qua endpoint public của backend.
    public async Task PlaySound(int audioId)
    {
        if (audioId <= 0)
        {
            return;
        }

        StopSound();
        _isPaused = false;

        try
        {
            _currentTrackKey = GetAudioTrackKey(audioId);

            await using var stream = await ResolvePlayableStreamAsync(audioId);
            if (stream == null)
            {
                _currentTrackKey = null;
                return;
            }

            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _player = _audioManager.CreatePlayer(memoryStream);
            _player.PlaybackEnded += OnPlaybackEnded;
            RequestPlatformAudioFocus();
            _player.Play();
        }
        catch (Exception)
        {
            _currentTrackKey = null;
        }
    }

    // Resolve stream phát được theo (language, fileName): local cache -> package -> fallback local sau lưu cache.
    private async Task<Stream?> ResolvePlayableStreamAsync(string language, string fileName)
    {
        var normalizedInput = NormalizeInput(fileName);
        var cachePath = GetAudioCachePath(language, normalizedInput);

        if (IsValidAudioFile(cachePath))
        {
            TouchCacheFile(cachePath);
            return File.OpenRead(cachePath);
        }

        foreach (var packagePath in BuildPackagePathCandidates(language, normalizedInput))
        {
            try
            {
                await using var packageStream = await FileSystem.OpenAppPackageFileAsync(packagePath);
                var memory = new MemoryStream();
                await packageStream.CopyToAsync(memory);
                memory.Position = 0;

                await SaveAudioCacheAsync(cachePath, memory);
                memory.Position = 0;
                return memory;
            }
            catch
            {
                // Continue with next candidate.
            }
        }

        return IsValidAudioFile(cachePath)
            ? File.OpenRead(cachePath)
            : null;
    }

    // Resolve stream phát được theo audioId: local cache -> remote endpoint -> fallback local sau khi tải.
    private async Task<Stream?> ResolvePlayableStreamAsync(int audioId)
    {
        if (audioId <= 0)
        {
            return null;
        }

        var cachePath = GetAudioCachePath(audioId);

        if (IsValidAudioFile(cachePath))
        {
            TouchCacheFile(cachePath);
            return File.OpenRead(cachePath);
        }

        foreach (var remoteUrl in BuildRemoteAudioUrlCandidates(audioId))
        {
            if (!await TryDownloadAudioToCacheAsync(remoteUrl, cachePath))
            {
                Debug.WriteLine($"[AudioService] Prefetch remote failed: {remoteUrl}");
                continue;
            }

            if (IsValidAudioFile(cachePath))
            {
                TouchCacheFile(cachePath);
                return File.OpenRead(cachePath);
            }
        }

        return IsValidAudioFile(cachePath)
            ? File.OpenRead(cachePath)
            : null;
    }

    // hàm này được dùng để kiểm tra xem có audio nào tồn tại trong local cache dựa trên ngôn ngữ và tên file hay không. Nó sẽ xây dựng đường dẫn cache tương ứng và kiểm tra xem file đó có tồn tại và hợp lệ hay không.
    public bool HasLocalAudio(string language, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var normalizedInput = NormalizeInput(fileName);
        var cachePath = GetAudioCachePath(language, normalizedInput);
        return IsValidAudioFile(cachePath);
    }

    // hàm này được dùng để kiểm tra xem có audio nào tồn tại trong local cache dựa trên audioId hay không. Nó sẽ xây dựng đường dẫn cache tương ứng với audioId và kiểm tra xem file đó có tồn tại và hợp lệ hay không.
    public bool HasLocalAudio(int audioId)
    {
        if (audioId <= 0)
        {
            return false;
        }

        var cachePath = GetAudioCachePath(audioId);
        return IsValidAudioFile(cachePath);
    }

    // Prefetch audio theo (language, fileName) để lần phát sau không bị chờ tải.
    public async Task<bool> PrefetchAudioAsync(string language, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var normalizedInput = NormalizeInput(fileName);
        var cachePath = GetAudioCachePath(language, normalizedInput);

        if (IsValidAudioFile(cachePath))
        {
            TouchCacheFile(cachePath);
            Debug.WriteLine($"[AudioService] Prefetch hit local cache: {language} | {fileName}");
            return true;
        }

        foreach (var packagePath in BuildPackagePathCandidates(language, normalizedInput))
        {
            try
            {
                await using var packageStream = await FileSystem.OpenAppPackageFileAsync(packagePath);
                var memory = new MemoryStream();
                await packageStream.CopyToAsync(memory);
                memory.Position = 0;

                await SaveAudioCacheAsync(cachePath, memory);
                if (IsValidAudioFile(cachePath))
                {
                    TouchCacheFile(cachePath);
                    Debug.WriteLine($"[AudioService] Prefetch from package success: {language} | {packagePath}");
                    return true;
                }
            }
            catch
            {
                // Continue with next candidate.
            }
        }

        foreach (var remoteUrl in BuildRemoteUrlCandidates(language, normalizedInput))
        {
            if (!await TryDownloadAudioToCacheAsync(remoteUrl, cachePath))
            {
                Debug.WriteLine($"[AudioService] Prefetch remote failed: {remoteUrl}");
                continue;
            }

            if (IsValidAudioFile(cachePath))
            {
                TouchCacheFile(cachePath);
                Debug.WriteLine($"[AudioService] Prefetch remote success: {remoteUrl}");
                return true;
            }
        }

        Debug.WriteLine($"[AudioService] Prefetch failed for all sources: {language} | {fileName}");
        return false;
    }

    // Prefetch audio theo audioId từ endpoint remote và lưu vào cache cục bộ.
    public async Task<bool> PrefetchAudioAsync(int audioId)
    {
        if (audioId <= 0)
        {
            return false;
        }

        var cachePath = GetAudioCachePath(audioId);

        if (IsValidAudioFile(cachePath))
        {
            TouchCacheFile(cachePath);
            Debug.WriteLine($"[AudioService] Prefetch hit local cache: audioId={audioId}");
            return true;
        }

        foreach (var remoteUrl in BuildRemoteAudioUrlCandidates(audioId))
        {
            if (!await TryDownloadAudioToCacheAsync(remoteUrl, cachePath))
            {
                Debug.WriteLine($"[AudioService] Prefetch remote failed: {remoteUrl}");
                continue;
            }

            if (IsValidAudioFile(cachePath))
            {
                TouchCacheFile(cachePath);
                Debug.WriteLine($"[AudioService] Prefetch remote success: {remoteUrl}");
                return true;
            }
        }

        Debug.WriteLine($"[AudioService] Prefetch failed for audioId={audioId}");
        return false;
    }

    // Chuẩn hóa path audio đầu vào thành path nội bộ thống nhất để so khớp và cache.
    private static string ResolveAudioPath(string language, string fileName)
    {
        var normalized = NormalizeInput(fileName);

        if (normalized.StartsWith("audio/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("narration/", StringComparison.OrdinalIgnoreCase)
            || normalized.StartsWith("resources/narration/", StringComparison.OrdinalIgnoreCase))
        {
            return normalized
                .Replace("resources/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("narration/", "audio/", StringComparison.OrdinalIgnoreCase);
        }

        if (normalized.Contains('/'))
        {
            return normalized;
        }

        return $"audio/languages/{language}/{normalized}";
    }

    // Chuẩn hóa chuỗi đầu vào (slash + trim) trước khi dùng cho cache/path building.
    private static string NormalizeInput(string fileName)
    {
        return fileName
            .Replace("\\", "/", StringComparison.Ordinal)
            .Trim();
    }

    // Sinh track key theo audioId để quản lý trạng thái current track.
    private static string GetAudioTrackKey(int audioId)
    {
        return $"audio:{audioId}";
    }

    // Tạo đường dẫn cache cho audioId bằng khóa hash ổn định.
    private static string GetAudioCachePath(int audioId)
    {
        var cacheRoot = GetAudioCacheRootPath();
        Directory.CreateDirectory(cacheRoot);

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"audio:{audioId}")));
        return Path.Combine(cacheRoot, $"{hash}.mp3");
    }

    // Build danh sách path trong app package có thể chứa audio tương ứng.
    private static IEnumerable<string> BuildPackagePathCandidates(string language, string normalizedInput)
    {
        var candidates = new List<string>();
        var resolved = ResolveAudioPath(language, normalizedInput);
        candidates.Add(resolved);

        // Some API payloads can include a relative app-package-like path.
        if (!candidates.Contains(normalizedInput, StringComparer.OrdinalIgnoreCase))
        {
            candidates.Add(normalizedInput);
        }

        return candidates
            .Where(x => !x.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                && !x.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    // Build danh sách URL remote theo (language, path) dựa trên base URL chính và fallback.
    private IEnumerable<string> BuildRemoteUrlCandidates(string language, string normalizedInput)
    {
        if (Uri.TryCreate(normalizedInput, UriKind.Absolute, out var directUri)
            && (directUri.Scheme == Uri.UriSchemeHttp || directUri.Scheme == Uri.UriSchemeHttps))
        {
            return new[] { directUri.ToString() };
        }

        var relativePath = ResolveAudioPath(language, normalizedInput);
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
                    return new Uri(new Uri(baseUrl), relativePath).ToString();
                }
                catch
                {
                    return string.Empty;
                }
            })
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    // Tạo đường dẫn cache cho (language, input) bằng hash để tránh trùng tên file.
    private static string GetAudioCachePath(string language, string normalizedInput)
    {
        var cacheRoot = GetAudioCacheRootPath();
        Directory.CreateDirectory(cacheRoot);

        var extension = Path.GetExtension(normalizedInput);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".mp3";
        }

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(
            $"{language}|{normalizedInput.ToLowerInvariant()}")));

        return Path.Combine(cacheRoot, $"{hash}{extension}");
    }

    // Trả về thư mục cache audio cục bộ trong AppDataDirectory.
    private static string GetAudioCacheRootPath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, AudioCacheFolderName);
    }

    // Kiểm tra file cache có tồn tại và đủ kích thước tối thiểu để phát an toàn.
    private static bool IsValidAudioFile(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            return new FileInfo(path).Length >= MinValidAudioBytes;
        }
        catch
        {
            return false;
        }
    }

    // Lưu file audio vào cache một cách an toàn (check quota/dung lượng + ghi file tạm tránh file hỏng).
    private async Task SaveAudioCacheAsync(string cachePath, Stream source)
    {
        var expectedBytes = source.CanSeek ? source.Length : MinValidAudioBytes;
        if (!await EnsureStorageForIncomingFileAsync(expectedBytes, cachePath))
        {
            // Console.WriteLine("Skip caching audio: storage constraints.");
            return;
        }

        source.Position = 0;
        var tempPath = $"{cachePath}.tmp";

        try
        {
            await using (var output = File.Create(tempPath))
            {
                await source.CopyToAsync(output);
            }

            var size = new FileInfo(tempPath).Length;
            if (size < MinValidAudioBytes)
            {
                File.Delete(tempPath);
                return;
            }

            if (!await EnsureStorageForIncomingFileAsync(size, cachePath))
            {
                File.Delete(tempPath);
                // Console.WriteLine("Skip caching audio after write: storage constraints.");
                return;
            }

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            File.Move(tempPath, cachePath);
            TouchCacheFile(cachePath);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            throw;
        }
    }

    // Tải audio từ URL; nếu đang ở UI thread thì đẩy sang background để tránh lag khung hình.
    private async Task<bool> TryDownloadAudioToCacheAsync(string url, string cachePath)
    {
        if (MainThread.IsMainThread)
        {
            return await Task.Run(() => TryDownloadAudioToCacheCoreAsync(url, cachePath));
        }

        return await TryDownloadAudioToCacheCoreAsync(url, cachePath);
    }

    // Core download: tải stream, validate kích thước, rồi commit vào cache bằng file tạm.
    private async Task<bool> TryDownloadAudioToCacheCoreAsync(string url, string cachePath)
    {
        try
        {
            Debug.WriteLine($"[AudioService] Download start: {url}");
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                Debug.WriteLine($"[AudioService] Download HTTP fail ({(int)response.StatusCode}): {url}");
                return false;
            }

            var declaredLength = response.Content.Headers.ContentLength ?? 0;
            Debug.WriteLine($"[AudioService] Download response OK: {url} | contentLength={declaredLength}");
            if (declaredLength > 0 && !await EnsureStorageForIncomingFileAsync(declaredLength, cachePath))
            {
                Debug.WriteLine($"[AudioService] Skip download (quota/storage pre-check): {url}");
                return false;
            }

            await using var source = await response.Content.ReadAsStreamAsync();
            var tempPath = $"{cachePath}.download";

            await using (var output = File.Create(tempPath))
            {
                await source.CopyToAsync(output);
            }

            var size = new FileInfo(tempPath).Length;
            if (size < MinValidAudioBytes)
            {
                File.Delete(tempPath);
                Debug.WriteLine($"[AudioService] Download too small ({size} bytes): {url}");
                return false;
            }

            if (!await EnsureStorageForIncomingFileAsync(size, cachePath))
            {
                File.Delete(tempPath);
                Debug.WriteLine($"[AudioService] Skip download after write (quota/storage): {url}");
                return false;
            }

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            File.Move(tempPath, cachePath);
            TouchCacheFile(cachePath);
            Debug.WriteLine($"[AudioService] Download success and cached: {url}");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[AudioService] Download exception: {url} -> {ex.Message}");
            return false;
        }
    }

    // Trả về tổng dung lượng (bytes) của toàn bộ audio đang cache trên thiết bị.
    public Task<long> GetCachedAudioSizeBytesAsync()
    {
        return Task.FromResult(GetCacheSizeBytes());
    }

    // Xóa toàn bộ file audio cache trong thư mục local cache.
    public Task ClearAudioCacheAsync()
    {
        try
        {
            var cacheRoot = GetAudioCacheRootPath();
            if (!Directory.Exists(cacheRoot))
            {
                return Task.CompletedTask;
            }

            foreach (var file in new DirectoryInfo(cacheRoot).GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    file.Delete();
                }
                catch (Exception)
                {
                    // Console.WriteLine($"Delete cache file failed ({file.Name}): {ex.Message}");
                }
            }
        }
        catch (Exception)
        {
            // Console.WriteLine($"Clear audio cache failed: {ex.Message}");
        }

        return Task.CompletedTask;
    }

    // Kiểm tra đủ quota (giới hạn tối đa tài nguyên mà bạn cho phép sử dụng) + đủ dung lượng trống thiết bị trước khi nhận thêm file mới vào cache.
    private async Task<bool> EnsureStorageForIncomingFileAsync(long incomingBytes, string protectedPath)
    {
        if (incomingBytes <= 0)
        {
            incomingBytes = MinValidAudioBytes;
        }

        if (incomingBytes > MaxAudioCacheBytes)
        {
            Debug.WriteLine($"[AudioService] Storage check fail: incoming={incomingBytes} exceeds quota={MaxAudioCacheBytes}");
            return false;
        }

        var hasQuotaCapacity = EnsureQuotaCapacity(incomingBytes, protectedPath);
        if (!hasQuotaCapacity)
        {
            Debug.WriteLine($"[AudioService] Storage check fail: quota capacity unavailable for incoming={incomingBytes}");
            return false;
        }

        var availableSpace = TryGetAvailableSpaceBytes();
        if (availableSpace.HasValue && availableSpace.Value <= 0)
        {
            // Some Android environments can report 0 even when storage is usable.
            availableSpace = null;
        }

        if (availableSpace.HasValue && availableSpace.Value < incomingBytes + MinDeviceFreeSpaceBytes)
        {
            var needToFree = incomingBytes + MinDeviceFreeSpaceBytes - availableSpace.Value;
            CleanupLruBytes(needToFree, protectedPath);
            availableSpace = TryGetAvailableSpaceBytes();
            if (availableSpace.HasValue && availableSpace.Value <= 0)
            {
                availableSpace = null;
            }
        }

        if (availableSpace.HasValue && availableSpace.Value < incomingBytes + MinDeviceFreeSpaceBytes)
        {
            Debug.WriteLine($"[AudioService] Storage check fail: free={availableSpace.Value}, needAtLeast={incomingBytes + MinDeviceFreeSpaceBytes}");
            return false;
        }

        await Task.CompletedTask;
        return true;
    }

    // Đảm bảo tổng cache không vượt quota bằng cách dọn file cũ theo LRU khi cần.
    private bool EnsureQuotaCapacity(long incomingBytes, string protectedPath)
    {
        var existingLength = 0L;
        if (File.Exists(protectedPath))
        {
            try
            {
                existingLength = new FileInfo(protectedPath).Length;
            }
            catch
            {
                existingLength = 0;
            }
        }

        var currentCacheSize = GetCacheSizeBytes();
        var projectedSize = currentCacheSize - existingLength + incomingBytes;
        if (projectedSize <= MaxAudioCacheBytes)
        {
            return true;
        }

        var needToFree = projectedSize - MaxAudioCacheBytes;
        CleanupLruBytes(needToFree, protectedPath);

        currentCacheSize = GetCacheSizeBytes();
        projectedSize = currentCacheSize - existingLength + incomingBytes;

        if (projectedSize > MaxAudioCacheBytes)
        {
            Debug.WriteLine($"[AudioService] Quota check fail: projected={projectedSize}, quota={MaxAudioCacheBytes}");
            return false;
        }

        return true;
    }

    // Dọn cache theo LRU cho đến khi giải phóng đủ số bytes yêu cầu.
    private long CleanupLruBytes(long bytesNeeded, string protectedPath)
    {
        if (bytesNeeded <= 0)
        {
            return 0;
        }

        var freed = 0L;
        foreach (var file in EnumerateCacheFilesByLru())
        {
            if (string.Equals(file.FullName, protectedPath, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                var len = file.Length;
                file.Delete();
                freed += len;

                if (freed >= bytesNeeded)
                {
                    break;
                }
            }
            catch (Exception)
            {
                // Console.WriteLine($"Cannot delete cache file ({file.Name}): {ex.Message}");
            }
        }

        return freed;
    }

    // Liệt kê file cache theo thứ tự ít dùng gần đây nhất (LRU).
    private static IEnumerable<FileInfo> EnumerateCacheFilesByLru()
    {
        var cacheRoot = GetAudioCacheRootPath();
        if (!Directory.Exists(cacheRoot))
        {
            return Enumerable.Empty<FileInfo>();
        }

        return new DirectoryInfo(cacheRoot)
            .GetFiles("*", SearchOption.TopDirectoryOnly)
            .Where(f => !f.Name.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase)
                && !f.Name.EndsWith(".download", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f.LastAccessTimeUtc)
            .ThenBy(f => f.LastWriteTimeUtc)
            .ToList();
    }

    // Tính tổng dung lượng cache hiện tại.
    private static long GetCacheSizeBytes()
    {
        return EnumerateCacheFilesByLru().Sum(f =>
        {
            try
            {
                return f.Length;
            }
            catch
            {
                return 0L;
            }
        });
    }

    // Build endpoint URL theo audioId để phát/tải từ backend.
    private IEnumerable<string> BuildRemoteAudioUrlCandidates(int audioId)
    {
        var relativePath = $"public/audios/{audioId}/file";
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
                    return new Uri(new Uri(baseUrl), relativePath).ToString();
                }
                catch
                {
                    return string.Empty;
                }
            })
            .Where(x => !string.IsNullOrWhiteSpace(x));
    }

    // Lấy dung lượng trống của ổ chứa cache; null nếu không xác định được.
    private static long? TryGetAvailableSpaceBytes()
    {
        try
        {
            var cacheRoot = GetAudioCacheRootPath();
            var root = Path.GetPathRoot(cacheRoot);
            if (string.IsNullOrWhiteSpace(root))
            {
                return null;
            }

            var drive = new DriveInfo(root);
            return drive.AvailableFreeSpace;
        }
        catch
        {
            return null;
        }
    }

    // Cập nhật access/write time để phản ánh file vừa được dùng (phục vụ LRU).
    private static void TouchCacheFile(string path)
    {
        try
        {
            var now = DateTime.UtcNow;
            File.SetLastAccessTimeUtc(path, now);
            File.SetLastWriteTimeUtc(path, now);
        }
        catch
        {
            // Ignore access-time update failures.
        }
    }

    // Tạm dừng audio hiện tại nếu đang phát.
    public void Pause()
    {
        if (_player is null || !_player.IsPlaying) return;
        _player.Pause();
        _isPaused = true;
    }

    // Tiếp tục phát audio đã tạm dừng.
    public void Resume()
    {
        if (_player is null || !_isPaused) return;
        _player.Play();
        _isPaused = false;
    }

    // Kiểm tra track hiện tại có khớp với (language, fileName) hay không.
    public bool IsCurrentTrack(string language, string fileName)
    {
        if (string.IsNullOrWhiteSpace(_currentTrackKey) || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var resolved = ResolveAudioPath(language, fileName);
        return string.Equals(_currentTrackKey, resolved, StringComparison.OrdinalIgnoreCase);
    }

    // Kiểm tra track hiện tại có khớp audioId hay không.
    public bool IsCurrentTrack(int audioId)
    {
        if (audioId <= 0 || string.IsNullOrWhiteSpace(_currentTrackKey))
        {
            return false;
        }

        return string.Equals(_currentTrackKey, GetAudioTrackKey(audioId), StringComparison.OrdinalIgnoreCase);
    }

    // Callback khi audio phát xong: reset state + nhả audio focus + bắn event kết thúc.
    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        ReleasePlatformAudioFocus();
        _isPaused = false;
        _currentTrackKey = null;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    // Dừng phát ngay lập tức và dọn toàn bộ trạng thái playback hiện tại.
    public void StopSound()
    {
        if (_player != null)
        {
            _player.PlaybackEnded -= OnPlaybackEnded;
        }

        _player?.Stop();
        _player = null;
        ReleasePlatformAudioFocus();
        _isPaused = false;
        _currentTrackKey = null;
    }

    // Dừng phát khi bị ngắt do platform interruption (call/app khác/cướp focus).
    internal void StopForPlatformInterruption()
    {
        if (!IsPlaying && !IsPaused)
        {
            return;
        }

        StopSound();
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }
}

