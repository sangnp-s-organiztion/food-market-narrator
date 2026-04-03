using Plugin.Maui.Audio;
using food_market_narrator.Settings;
using System.Security.Cryptography;
using System.Diagnostics;

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

    public bool HasLocalAudio(int audioId)
    {
        if (audioId <= 0)
        {
            return false;
        }

        var cachePath = GetAudioCachePath(audioId);
        return IsValidAudioFile(cachePath);
    }

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

    private static string GetAudioTrackKey(int audioId)
    {
        return $"audio:{audioId}";
    }

    private static string GetAudioCachePath(int audioId)
    {
        var cacheRoot = GetAudioCacheRootPath();
        Directory.CreateDirectory(cacheRoot);

        var hash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes($"audio:{audioId}")));
        return Path.Combine(cacheRoot, $"{hash}.mp3");
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
        if (MainThread.IsMainThread)
        {
            return await Task.Run(() => TryDownloadAudioToCacheCoreAsync(url, cachePath));
        }

        return await TryDownloadAudioToCacheCoreAsync(url, cachePath);
    }

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

    public bool IsCurrentTrack(int audioId)
    {
        if (audioId <= 0 || string.IsNullOrWhiteSpace(_currentTrackKey))
        {
            return false;
        }

        return string.Equals(_currentTrackKey, GetAudioTrackKey(audioId), StringComparison.OrdinalIgnoreCase);
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
}

