using Microsoft.Maui.Devices.Sensors;
using food_market_narrator.Models;

namespace food_market_narrator.Services;

public class NarrationFlowService : INarrationFlowService
{
    private readonly POIService _poiService;
    private readonly ILocationService _locationService;
    private readonly IAudioService _audioService;
    private readonly ILanguageService _languageService;

    private readonly HashSet<string> _playedPOIs = new();

    private const double TRIGGER_DISTANCE_METERS = 30;

    private bool _isNarrationEnabled = false;

    private readonly Queue<POI> _playQueue = new();
    private bool _isProcessingQueue = false;
    public bool IsNarrating => _isNarrationEnabled;

    public NarrationFlowService(
        POIService poiService,
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
        Console.WriteLine($"IsNarrating: {_isNarrationEnabled}");
        _locationService.LocationChanged += OnLocationChanged;
        await _locationService.StartTrackingAsync();

        // 👇 THÊM DÒNG NÀY
        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation != null)
        {
            await CheckAndNarrateAsync(currentLocation);
        }

        Console.WriteLine("Narration STARTED");
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

        // reset POI đã phát để lần Start thủ công tiếp theo có thể phát lại ở cùng vị trí
        _playedPOIs.Clear();

        Console.WriteLine("Narration STOPPED");
    }

    // Khi thay đổi vị trí thì làm gì đó
    private async void OnLocationChanged(object? sender, Location location)
    {
        await CheckAndNarrateAsync(location);
    }

    public async Task CheckAndNarrateAsync(Location? currentLocation = null, bool force = false)
    {
        Console.WriteLine("=== CHECK NARRATE START ===");

        //if (_audioService.IsPlaying)
        //{
        //    Console.WriteLine("Audio đang phát, skip...");
        //    return;
        //}

        if (currentLocation == null)
            currentLocation = await _locationService.GetCurrentLocationAsync();

        if (currentLocation == null)
        {
            Console.WriteLine("Current location NULL");
            return;
        }

        //Console.WriteLine($"Current location: {currentLocation.Item1}, {currentLocation.Item2}");

        var pois = await _poiService.GetAllPOIsAsync();
        if (pois == null || !pois.Any())
        {
            Console.WriteLine("POI list empty");
            return;
        }

        //Console.WriteLine($"POI COUNT: {pois.Count()}");

        var nearestPOI = pois
            .Select(p => new
            {
                POI = p,
                Distance = Location.CalculateDistance(
                    currentLocation,
                    new Location(p.Latitude, p.Longitude),
                    DistanceUnits.Kilometers)
            })
            .OrderBy(x => x.Distance)
            .FirstOrDefault();

        if (nearestPOI == null)
        {
            Console.WriteLine("Nearest POI NULL - No valid POI found");
            return;
        }

        var selectedAudio = nearestPOI.POI.GetAudioUrl(_languageService.CurrentLanguage);

        if (string.IsNullOrWhiteSpace(selectedAudio))
        {
            Console.WriteLine($"No audio found for POI: {nearestPOI.POI.Name}");
            return;
        }

        double distanceMeters = nearestPOI.Distance * 1000;
        Console.WriteLine($"Nearest POI: {nearestPOI.POI.restaurantId} - {distanceMeters:F1}m");

        // Nếu force (manual trigger) hoặc trong khoảng cách cho phép
        if (force || distanceMeters <= TRIGGER_DISTANCE_METERS)
        {
            Console.WriteLine(force ? "Manual trigger activated" : "Inside trigger radius");

            var poiId = nearestPOI.POI.restaurantId;
            var alreadyPlayed = _playedPOIs.Contains(poiId);

            // Force luôn cho phép phát lại POI hiện tại
            if (force || !alreadyPlayed)
            {
                Console.WriteLine("Playing audio...");

                Console.WriteLine("Add to queue...");
                _playQueue.Enqueue(nearestPOI.POI);

                if (!alreadyPlayed)
                {
                    _playedPOIs.Add(poiId);
                }

                await ProcessQueueAsync();
            }
            else
            {
                Console.WriteLine("POI already played (auto-trigger skipped)");
            }
        }
        else
        {
            Console.WriteLine($"Too far from nearest POI ({distanceMeters:F1}m > {TRIGGER_DISTANCE_METERS}m)");
        }

        Console.WriteLine("=== CHECK NARRATE END ===");
    }

    private async Task ProcessQueueAsync()
    {
        if (_isProcessingQueue)
            return;

        _isProcessingQueue = true;

        while (_playQueue.Count > 0)
        {
            var poi = _playQueue.Dequeue();

            Console.WriteLine($"Queue playing: {poi.Name}");

            var selectedAudio = poi.GetAudioUrl(_languageService.CurrentLanguage);
            if (string.IsNullOrWhiteSpace(selectedAudio))
            {
                Console.WriteLine($"No playable audio for POI: {poi.Name}");
                continue;
            }

            await _audioService.PlaySound(
                _languageService.CurrentLanguage,
                selectedAudio
            );

            // đợi audio phát xong
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