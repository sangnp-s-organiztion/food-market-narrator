using Microsoft.Maui.Devices.Sensors;
using food_market_narrator.Models;
using food_market_narrator.Settings;

namespace food_market_narrator.Services;

public class NarrationFlowService : INarrationFlowService
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private readonly IAudioService _audioService;
    private readonly ILanguageService _languageService;

    private readonly HashSet<string> _playedPOIs = new();

    private bool _isNarrationEnabled = false;

    private readonly Queue<POI> _playQueue = new();
    private bool _isProcessingQueue = false;
    public bool IsNarrating => _isNarrationEnabled;

    public NarrationFlowService(
        IPOIService poiService,
        ILocationService locationService,
        IAudioService audioService,
        ILanguageService languageService)
    {
        _poiService = poiService;
        _locationService = locationService;
        _audioService = audioService;
        _languageService = languageService;
        
        // Subscribe to location updates
        //_locationService.LocationChanged += OnLocationChanged;
    }

    public async void StartNarration()
    {
        if (_isNarrationEnabled) return;

        _isNarrationEnabled = true;
        // Console.WriteLine($"IsNarrating: {_isNarrationEnabled}");
        _locationService.LocationChanged += OnLocationChanged;
        await _locationService.StartTrackingAsync();

        // ðŸ‘‡ THÃŠM DÃ’NG NÃ€Y
        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation != null)
        {
            await CheckAndNarrateAsync(currentLocation);
        }

        // Console.WriteLine("Narration STARTED");
    }

    public void StopNarration()
    {
        if (!_isNarrationEnabled) return;

        _isNarrationEnabled = false;

        // stop tracking
        _locationService.LocationChanged -= OnLocationChanged;

        // stop audio
        _audioService.StopSound();

        // clear queue
        _playQueue.Clear();
        _isProcessingQueue = false;

        // reset POI Ä‘Ã£ phÃ¡t Ä‘á»ƒ láº§n Start thá»§ cÃ´ng tiáº¿p theo cÃ³ thá»ƒ phÃ¡t láº¡i á»Ÿ cÃ¹ng vá»‹ trÃ­
        _playedPOIs.Clear();

        // Console.WriteLine("Narration STOPPED");
    }

    // Khi thay Ä‘á»•i vá»‹ trÃ­ thÃ¬ lÃ m gÃ¬ Ä‘Ã³
    private async void OnLocationChanged(object? sender, Location location)
    {
        await CheckAndNarrateAsync(location);
    }

    public async Task CheckAndNarrateAsync(Location? currentLocation = null, bool force = false)
    {
        // Console.WriteLine("=== CHECK NARRATE START ===");

        //if (_audioService.IsPlaying)
        //{
        //    Console.WriteLine("Audio Ä‘ang phÃ¡t, skip...");
        //    return;
        //}

        if (currentLocation == null)
            currentLocation = await _locationService.GetCurrentLocationAsync();

        if (currentLocation == null)
        {
            // Console.WriteLine("Current location NULL");
            return;
        }

        //Console.WriteLine($"Current location: {currentLocation.Item1}, {currentLocation.Item2}");

        var pois = await _poiService.GetAllPOIsAsync();
        if (pois == null || !pois.Any())
        {
            // Console.WriteLine("POI list empty");
            return;
        }

        //Console.WriteLine($"POI COUNT: {pois.Count()}");

        var nearestPoi = _poiService.GetNearestPOI(currentLocation, pois);

        if (nearestPoi == null)
        {
            // Console.WriteLine("Nearest POI NULL - No valid POI found");
            return;
        }

        var selectedAudio = nearestPoi.GetAudioUrl(_languageService.CurrentLanguage);

        if (string.IsNullOrWhiteSpace(selectedAudio))
        {
            // Console.WriteLine($"No audio found for POI: {nearestPoi.Name}");
            return;
        }

        var distanceMeters = _poiService.GetDistanceMeters(currentLocation, nearestPoi);
        // Console.WriteLine($"Nearest POI: {nearestPoi.restaurantId} - {distanceMeters:F1}m");

        // Náº¿u force (manual trigger) hoáº·c trong khoáº£ng cÃ¡ch cho phÃ©p
        if (force || distanceMeters <= AppSettings.TriggerDistanceMeters)
        {
            // Console.WriteLine(force ? "Manual trigger activated" : "Inside trigger radius");

            var poiId = nearestPoi.restaurantId;
            var alreadyPlayed = _playedPOIs.Contains(poiId);

            // Force luÃ´n cho phÃ©p phÃ¡t láº¡i POI hiá»‡n táº¡i
            if (force || !alreadyPlayed)
            {
                // Console.WriteLine("Playing audio...");

                // Console.WriteLine("Add to queue...");
                _playQueue.Enqueue(nearestPoi);

                if (!alreadyPlayed)
                {
                    _playedPOIs.Add(poiId);
                }

                await ProcessQueueAsync();
            }
            else
            {
                // Console.WriteLine("POI already played (auto-trigger skipped)");
            }
        }
        else
        {
            // Console.WriteLine($"Too far from nearest POI ({distanceMeters:F1}m > {AppSettings.TriggerDistanceMeters}m)");
        }

        // Console.WriteLine("=== CHECK NARRATE END ===");
    }

    private async Task ProcessQueueAsync()
    {
        if (_isProcessingQueue)
            return;

        _isProcessingQueue = true;

        while (_playQueue.Count > 0)
        {
            var poi = _playQueue.Dequeue();

            // Console.WriteLine($"Queue playing: {poi.Name}");

            var selectedAudio = poi.GetAudioUrl(_languageService.CurrentLanguage);
            if (string.IsNullOrWhiteSpace(selectedAudio))
            {
                // Console.WriteLine($"No playable audio for POI: {poi.Name}");
                continue;
            }

            await _audioService.PlaySound(
                _languageService.CurrentLanguage,
                selectedAudio
            );

            // Ä‘á»£i audio phÃ¡t xong
            while (_audioService.IsPlaying)
            {
                await Task.Delay(300);
            }
        }

        _isProcessingQueue = false;
    }

    public void ResetPlayedPOIs()
    {
        _playedPOIs.Clear();
    }
}
