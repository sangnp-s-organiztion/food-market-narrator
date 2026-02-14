using Microsoft.Maui.Devices.Sensors;
using food_market_narrator.Models;
using food_market_narrator.Services;
using food_market_narrator.Views;

public class NarrationFlowService
{
    private readonly POIService _poiService;
    private readonly LocationServices _locationService = new();
    private readonly AudioService _audioService = new();
    private readonly LanguageService _languageService = new();

    private readonly HashSet<string> _playedPOIs = new();

    private const double TRIGGER_DISTANCE_METERS = 30;
    public NarrationFlowService(POIService poiService)
    {
        _poiService = poiService;
    }

    public async Task CheckAndNarrateAsync()
{

    
    Console.WriteLine("=== CHECK NARRATE START ===");

    if (_audioService.IsPlaying)
    {
        Console.WriteLine("Audio đang phát, skip...");
        return;
    }

    var currentLocation = await _locationService.GetCurrentLocation();
    if (currentLocation.Item1 == null || currentLocation.Item2 == null)
    {
        Console.WriteLine("Current location NULL");
        return;
    }

    Console.WriteLine($"Current location: {currentLocation.Item1}, {currentLocation.Item2}");

    var pois = await _poiService.GetAllPOIsAsync();
    if (pois == null || !pois.Any())
    {
        Console.WriteLine("POI list empty");
        return;
    }

    Console.WriteLine($"POI COUNT: {pois.Count()}");

    var nearestPOI = pois
        .Select(p => new
        {
            POI = p,
            Distance = Location.CalculateDistance(
                new Location(currentLocation.Item1.Value, currentLocation.Item2.Value),
                new Location(p.Latitude, p.Longitude),
                DistanceUnits.Kilometers)
        })
        .OrderBy(x => x.Distance)
        .FirstOrDefault();

    if (string.IsNullOrWhiteSpace(nearestPOI.POI.AudioFile))
    {
        Console.WriteLine("AudioFile is NULL or EMPTY!");
        return;
    }

    if (nearestPOI == null)
    {
        Console.WriteLine("Nearest POI NULL");
        return;
    }

    double distanceMeters = nearestPOI.Distance * 1000;
    Console.WriteLine($"Nearest POI: {nearestPOI.POI.restaurantId} - {distanceMeters}m");

    if (distanceMeters <= 30)
    {
        Console.WriteLine("Inside trigger radius");

        if (!_playedPOIs.Contains(nearestPOI.POI.restaurantId))
        {
            Console.WriteLine("Playing audio...");
            await _audioService.PlaySound(
                _languageService.CurrentLanguage,
                nearestPOI.POI.AudioFile
            );
        }
        else
        {
            Console.WriteLine("POI already played");
        }
    }

    Console.WriteLine("=== CHECK NARRATE END ===");
}

    public void ResetPlayedPOIs()
    {
        _playedPOIs.Clear();
    }
}