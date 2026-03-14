using Plugin.Maui.Audio;
using food_market_narrator.Settings;
using System.Security.Cryptography;

namespace food_market_narrator.Services;

public class AudioService : IAudioService
{
    private readonly IAudioManager _audioManager;
    private readonly HttpClient _httpClient;
    private IAudioPlayer? _player;
    private bool _isPaused;
    private string? _currentTrackKey;
    private const int MinValidAudioBytes = 256;
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
    }


    // ================ Audio Methods ================

    public async Task PlaySound(string language, string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            Console.WriteLine("File name null -> skip");
            return;
        }

        StopSound();
        _isPaused = false;

        try
        {
            _currentTrackKey = ResolveAudioPath(language, fileName);
            Console.WriteLine($"Loading audio key: {_currentTrackKey}");

            await using var stream = await ResolvePlayableStreamAsync(language, fileName);
            if (stream == null)
            {
                Console.WriteLine($"Audio not found for input: {fileName}");
                _currentTrackKey = null;
                return;
            }

            var memoryStream = new MemoryStream();
            await stream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            _player = _audioManager.CreatePlayer(memoryStream);
            _player.PlaybackEnded += OnPlaybackEnded;
            _player.Play();

            Console.WriteLine("Audio started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR PLAY SOUND: {ex}");
            _currentTrackKey = null;
        }
    }

    private async Task<Stream?> ResolvePlayableStreamAsync(string language, string fileName)
    {
        var normalizedInput = NormalizeInput(fileName);
        var cachePath = GetAudioCachePath(language, normalizedInput);

        if (IsValidAudioFile(cachePath))
        {
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

        foreach (var remoteUrl in BuildRemoteUrlCandidates(language, normalizedInput))
        {
            if (!await TryDownloadAudioToCacheAsync(remoteUrl, cachePath))
            {
                continue;
            }

            if (IsValidAudioFile(cachePath))
            {
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
        var cacheRoot = Path.Combine(FileSystem.AppDataDirectory, "audio_cache");
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

    private static async Task SaveAudioCacheAsync(string cachePath, Stream source)
    {
        source.Position = 0;
        var tempPath = $"{cachePath}.tmp";

        await using (var output = File.Create(tempPath))
        {
            await source.CopyToAsync(output);
        }

        if (File.Exists(cachePath))
        {
            File.Delete(cachePath);
        }

        File.Move(tempPath, cachePath);
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

            if (File.Exists(cachePath))
            {
                File.Delete(cachePath);
            }

            File.Move(tempPath, cachePath);
            Console.WriteLine($"Audio downloaded and cached: {url}");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Download audio failed ({url}): {ex.Message}");
            return false;
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

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
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
        _isPaused = false;
        _currentTrackKey = null;
    }
}