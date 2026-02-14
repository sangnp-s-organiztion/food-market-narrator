
using food_market_narrator.Services;
using Plugin.Maui.Audio;

public class AudioService
{
    private POIService _poiService = new POIService();
    private LocationServices _locationService = new LocationServices();
    private LanguageService _languageService = new LanguageService();

    private readonly IAudioManager _audioManager;
    private IAudioPlayer? _player;
    public bool IsPlaying => _player?.IsPlaying ?? false;

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
            var path = $"audio/languages/{language}/{fileName}";
            Console.WriteLine($"Loading path: {path}");

            var stream = await FileSystem.OpenAppPackageFileAsync(path);
            _player = _audioManager.CreatePlayer(stream);

            _player.Play();
            Console.WriteLine("Audio started");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR PLAY SOUND: {ex}");
        }
    }

    public void StopSound()
    {
        // Implementation to stop sound
        _player?.Stop();
        _player = null;
    }
}