using Plugin.Maui.Audio;

namespace food_market_narrator.Services;

public class AudioService : IAudioService
{
    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;
    public bool IsPlaying => _player?.IsPlaying ?? false;
    public TimeSpan Duration => TimeSpan.FromSeconds(_player?.Duration ?? 0d);
    public TimeSpan CurrentPosition => TimeSpan.FromSeconds(_player?.CurrentPosition ?? 0d);
    public event EventHandler? PlaybackEnded;

    public AudioService()
        {
            _audioManager = AudioManager.Current;
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

        try
        {
            var path = ResolveAudioPath(language, fileName);
            Console.WriteLine($"Loading path: {path}");

            var stream = await FileSystem.OpenAppPackageFileAsync(path);
            // Lưu audio vừa load vào cache để phát lại nếu cần
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
        }
    }

    private static string ResolveAudioPath(string language, string fileName)
    {
        var normalized = fileName
            .Replace("\\", "/", StringComparison.Ordinal)
            .Trim();

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

    private void OnPlaybackEnded(object? sender, EventArgs e)
    {
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
    }
}