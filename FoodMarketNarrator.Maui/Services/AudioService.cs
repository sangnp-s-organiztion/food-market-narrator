using Plugin.Maui.Audio;
using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Security.Cryptography;

namespace food_market_narrator.Services;

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
    private readonly SemaphoreSlim _preloadLock = new(1, 1);
    private bool _preloadAttempted;
    public bool IsPlaying => _player?.IsPlaying ?? false;
    public bool IsPaused => _isPaused;
    public string? CurrentTrackKey => _currentTrackKey;
    public TimeSpan Duration => TimeSpan.FromSeconds(_player?.Duration ?? 0d);
    public TimeSpan CurrentPosition => TimeSpan.FromSeconds(_player?.CurrentPosition ?? 0d);
    public event EventHandler? PlaybackEnded;
    public event EventHandler<long>? CacheSizeChanged;

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

    public async Task PlaySound(string language, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] File name null -> skip");
            return;
        }

        StopSound();
        _isPaused = false;

        try
        {
            _currentTrackKey = ResolveAudioPath(language, fileName);
            System.Diagnostics.Debug.WriteLine($"[AudioService] Loading audio key: {_currentTrackKey}");

            await using var stream = await ResolvePlayableStreamAsync(language, fileName);
            if (stream == null)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Audio not found for input: {fileName}");
                _currentTrackKey = null;
                return;
            }

            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;
            System.Diagnostics.Debug.WriteLine($"[AudioService] Stream loaded, size={memoryStream.Length} bytes");

            // Validate: ensure stream looks like an audio file, not an HTML error page
            if (!IsLikelyAudioStream(memoryStream))
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Stream does NOT look like audio data (size={memoryStream.Length}). Possibly HTML error page.");
                memoryStream.Dispose();
                _currentTrackKey = null;
                return;
            }

            _player = _audioManager.CreatePlayer(memoryStream);
            _player.PlaybackEnded += OnPlaybackEnded;
            RequestPlatformAudioFocus();
            _player.Play();

            System.Diagnostics.Debug.WriteLine($"[AudioService] Audio started, IsPlaying={_player.IsPlaying}, Duration={_player.Duration}s");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] ERROR PLAY SOUND: {ex}");
            _currentTrackKey = null;
        }
    }

    public async Task PreloadAllActiveAudiosAsync(IEnumerable<POI> pois, CancellationToken cancellationToken = default)
    {
        if (pois == null)
        {
            return;
        }

        await _preloadLock.WaitAsync(cancellationToken);
        try
        {
            if (_preloadAttempted)
            {
                return;
            }

            _preloadAttempted = true;

            var candidates = pois
                .SelectMany(p => p.Audios)
                .Where(a => a.IsActive && !string.IsNullOrWhiteSpace(a.AudioUrl))
                .Select(a => new AudioPreloadTarget(a.LanguageCode, NormalizeInput(a.AudioUrl)))
                .Distinct()
                .ToList();

            WriteDownloadStatusLog($"PRELOAD_START count={candidates.Count}");

            if (candidates.Count == 0)
            {
                WriteDownloadStatusLog("PRELOAD_SKIP reason=no_candidates downloaded=false");
                return;
            }

            var resolvedTargets = new List<ResolvedAudioPreloadTarget>(candidates.Count);
            long totalBytes = 0;

            foreach (var candidate in candidates)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var resolved = await ResolveAudioSizeAndUrlAsync(candidate, cancellationToken);
                if (resolved == null)
                {
                    // Skip file này, tiếp tục file khác — không return cả preload
                    WriteDownloadStatusLog($"PRELOAD_SKIP reason=url_not_resolved target={candidate.AudioInput} downloaded=false");
                    continue;
                }

                totalBytes += resolved.SizeBytes;
                resolvedTargets.Add(resolved);
            }

            if (resolvedTargets.Count == 0)
            {
                WriteDownloadStatusLog($"PRELOAD_SKIP reason=no_urls_resolved downloaded=false");
                return;
            }

            var freeSpace = TryGetAvailableSpaceBytes();
            // Check: free space phải đủ cho totalBytes CỘNG thêm buffer MinDeviceFreeSpaceBytes
            var minRequiredFree = totalBytes + MinDeviceFreeSpaceBytes;
            if (!freeSpace.HasValue || freeSpace.Value < minRequiredFree)
            {
                WriteDownloadStatusLog(
                    $"PRELOAD_SKIP reason=insufficient_storage " +
                    $"free={freeSpace?.ToString() ?? "unknown"} " +
                    $"total_audio={totalBytes} " +
                    $"min_required_free={minRequiredFree} " +
                    $"buffer={MinDeviceFreeSpaceBytes} " +
                    $"downloaded=false");
                return;
            }

            WriteDownloadStatusLog($"PRELOAD_READY total={totalBytes} free={freeSpace.Value} downloaded=true");

            var successCount = 0;
            var failCount = 0;

            foreach (var target in resolvedTargets)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var cachePath = GetAudioCachePath(target.LanguageCode, target.AudioInput);
                if (IsValidAudioFile(cachePath))
                {
                    TouchCacheFile(cachePath);
                    WriteDownloadStatusLog($"PRELOAD_FILE status=already_cached target={target.AudioInput} downloaded=true");
                    successCount++;
                    continue;
                }

                var downloaded = await TryDownloadAudioToCacheAsync(target.DownloadUrl, cachePath);
                WriteDownloadStatusLog($"PRELOAD_FILE status={(downloaded ? "downloaded" : "failed")} target={target.AudioInput} downloaded={downloaded.ToString().ToLowerInvariant()}");
                if (downloaded) successCount++; else failCount++;
            }

            NotifyCacheSizeChanged();
            WriteDownloadStatusLog($"PRELOAD_DONE total_files={resolvedTargets.Count} success={successCount} failed={failCount} cache_size={GetCacheSizeBytes()}");
        }
        finally
        {
            _preloadLock.Release();
        }
    }

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
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AudioService] Package path failed '{packagePath}': {ex.Message}");
            }
        }

        foreach (var remoteUrl in BuildRemoteUrlCandidates(language, normalizedInput))
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Trying remote URL: {remoteUrl}");
            if (!await TryDownloadAudioToCacheAsync(remoteUrl, cachePath))
            {
                var onlineOnly = await TryDownloadAudioToMemoryAsync(remoteUrl);
                if (onlineOnly != null)
                {
                    System.Diagnostics.Debug.WriteLine($"[AudioService] Playing online-only audio (not cached): {remoteUrl}, size={onlineOnly.Length}");
                    return onlineOnly;
                }

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

    private static string NormalizeInput(string fileName)
    {
        return fileName
            .Replace("\\", "/", StringComparison.Ordinal)
            .Trim();
    }

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

    private static string GetAudioCacheRootPath()
    {
        return Path.Combine(FileSystem.AppDataDirectory, AudioCacheFolderName);
    }

    private static bool IsLikelyAudioStream(MemoryStream stream)
    {
        if (stream.Length < MinValidAudioBytes)
        {
            return false;
        }

        // Check for HTML error page markers in first 200 bytes
        var previewLength = (int)Math.Min(stream.Length, 200);
        var preview = new byte[previewLength];
        var originalPosition = stream.Position;
        stream.Position = 0;
        stream.Read(preview, 0, previewLength);
        stream.Position = originalPosition;

        var previewText = System.Text.Encoding.UTF8.GetString(preview);

        // Common HTML error page patterns
        if (previewText.Contains("<html", StringComparison.OrdinalIgnoreCase)
            || previewText.Contains("<!DOCTYPE", StringComparison.OrdinalIgnoreCase)
            || previewText.Contains("Not Found", StringComparison.OrdinalIgnoreCase)
            || previewText.Contains("404", StringComparison.OrdinalIgnoreCase)
            || previewText.Contains("Error", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

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
            NotifyCacheSizeChanged();
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

    private async Task<bool> TryDownloadAudioToCacheAsync(string url, string cachePath)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            var declaredLength = response.Content.Headers.ContentLength ?? 0;
            if (declaredLength > 0 && !await EnsureStorageForIncomingFileAsync(declaredLength, cachePath))
            {
                // Console.WriteLine($"Skip download cache (not enough storage/quota): {url}");
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
                return false;
            }

            if (!await EnsureStorageForIncomingFileAsync(size, cachePath))
            {
                File.Delete(tempPath);
                // Console.WriteLine($"Skip download cache after write (not enough storage/quota): {url}");
                return false;
            }

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            File.Move(tempPath, cachePath);
            TouchCacheFile(cachePath);
            NotifyCacheSizeChanged();
            // Console.WriteLine($"Audio downloaded and cached: {url}");
            WriteDownloadStatusLog($"DOWNLOAD status=success url={url} downloaded=true");
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AudioService] Download audio failed ({url}): {ex.Message}");
            WriteDownloadStatusLog($"DOWNLOAD status=failed url={url} downloaded=false");
            return false;
        }
    }

    private async Task<Stream?> TryDownloadAudioToMemoryAsync(string url)
    {
        try
        {
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            await using var source = await response.Content.ReadAsStreamAsync();
            var memory = new MemoryStream();
            await source.CopyToAsync(memory);

            if (memory.Length < MinValidAudioBytes)
            {
                memory.Dispose();
                return null;
            }

            memory.Position = 0;
            return memory;
        }
        catch
        {
            return null;
        }
    }

    private async Task<ResolvedAudioPreloadTarget?> ResolveAudioSizeAndUrlAsync(AudioPreloadTarget target, CancellationToken cancellationToken)
    {
        foreach (var candidateUrl in BuildRemoteUrlCandidates(target.LanguageCode, target.AudioInput))
        {
            cancellationToken.ThrowIfCancellationRequested();

            WriteDownloadStatusLog($"PRELOAD_RESOLVE url={candidateUrl} lang={target.LanguageCode} target={target.AudioInput}");
            var size = await TryGetRemoteFileSizeAsync(candidateUrl, cancellationToken);
            if (size.HasValue && size.Value >= MinValidAudioBytes)
            {
                WriteDownloadStatusLog($"PRELOAD_RESOLVE_SUCCESS size={size.Value} bytes url={candidateUrl}");
                return new ResolvedAudioPreloadTarget(target.LanguageCode, target.AudioInput, candidateUrl, size.Value);
            }
            WriteDownloadStatusLog($"PRELOAD_RESOLVE_FAILED size={size?.ToString() ?? "null"} url={candidateUrl}");
        }

        return null;
    }

    private async Task<long?> TryGetRemoteFileSizeAsync(string url, CancellationToken cancellationToken)
    {
        try
        {
            using var headRequest = new HttpRequestMessage(HttpMethod.Head, url);
            using var headResponse = await _httpClient.SendAsync(headRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (headResponse.IsSuccessStatusCode)
            {
                var contentLength = headResponse.Content.Headers.ContentLength;
                if (contentLength.HasValue && contentLength.Value > 0)
                {
                    return contentLength.Value;
                }
            }
        }
        catch (Exception ex)
        {
            WriteDownloadStatusLog($"PRELOAD_HEAD_FAILED url={url} error={ex.Message}");
        }

        try
        {
            using var getRequest = new HttpRequestMessage(HttpMethod.Get, url);
            using var getResponse = await _httpClient.SendAsync(getRequest, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!getResponse.IsSuccessStatusCode)
            {
                WriteDownloadStatusLog($"PRELOAD_GET_FAILED url={url} status={getResponse.StatusCode}");
                return null;
            }

            var contentLength = getResponse.Content.Headers.ContentLength;
            WriteDownloadStatusLog($"PRELOAD_GET_SUCCESS url={url} contentLength={contentLength}");
            return contentLength;
        }
        catch (Exception ex)
        {
            WriteDownloadStatusLog($"PRELOAD_GET_EXCEPTION url={url} error={ex.Message}");
            return null;
        }
    }

    public Task<long> GetCachedAudioSizeBytesAsync()
    {
        return Task.FromResult(GetCacheSizeBytes());
    }

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

        NotifyCacheSizeChanged();

        return Task.CompletedTask;
    }

    private async Task<bool> EnsureStorageForIncomingFileAsync(long incomingBytes, string protectedPath)
    {
        if (incomingBytes <= 0)
        {
            incomingBytes = MinValidAudioBytes;
        }

        if (incomingBytes > MaxAudioCacheBytes)
        {
            // Console.WriteLine($"Incoming audio ({incomingBytes} bytes) exceeds cache quota ({MaxAudioCacheBytes} bytes).");
            return false;
        }

        var hasQuotaCapacity = EnsureQuotaCapacity(incomingBytes, protectedPath);
        if (!hasQuotaCapacity)
        {
            return false;
        }

        var availableSpace = TryGetAvailableSpaceBytes();
        if (availableSpace.HasValue && availableSpace.Value < incomingBytes + MinDeviceFreeSpaceBytes)
        {
            var needToFree = incomingBytes + MinDeviceFreeSpaceBytes - availableSpace.Value;
            CleanupLruBytes(needToFree, protectedPath);
            availableSpace = TryGetAvailableSpaceBytes();
        }

        if (availableSpace.HasValue && availableSpace.Value < incomingBytes + MinDeviceFreeSpaceBytes)
        {
            // Console.WriteLine("Not enough free storage for audio cache write.");
            return false;
        }

        await Task.CompletedTask;
        return true;
    }

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
            // Console.WriteLine("Audio cache quota reached and could not free enough files.");
            return false;
        }

        return true;
    }

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

    private void NotifyCacheSizeChanged()
    {
        try
        {
            CacheSizeChanged?.Invoke(this, GetCacheSizeBytes());
        }
        catch
        {
            // Ignore observer exceptions.
        }
    }

    private static void WriteDownloadStatusLog(string message)
    {
        Console.WriteLine($"[AudioService] {message}");
    }

    public void Pause()
    {
        if (_player is null || !_player.IsPlaying) return;
        _player.Pause();
        _isPaused = true;
    }

    public void Resume()
    {
        if (_player is null || !_isPaused) return;
        _player.Play();
        _isPaused = false;
    }

    public bool IsCurrentTrack(string language, string fileName)
    {
        if (string.IsNullOrWhiteSpace(_currentTrackKey) || string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }

        var resolved = ResolveAudioPath(language, fileName);
        return string.Equals(_currentTrackKey, resolved, StringComparison.OrdinalIgnoreCase);
    }

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
        ReleasePlatformAudioFocus();
        _isPaused = false;
        _currentTrackKey = null;
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

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

    internal void StopForPlatformInterruption()
    {
        if (!IsPlaying && !IsPaused)
        {
            return;
        }

        StopSound();
        PlaybackEnded?.Invoke(this, EventArgs.Empty);
    }

    private sealed record AudioPreloadTarget(string LanguageCode, string AudioInput);
    private sealed record ResolvedAudioPreloadTarget(string LanguageCode, string AudioInput, string DownloadUrl, long SizeBytes);
}

