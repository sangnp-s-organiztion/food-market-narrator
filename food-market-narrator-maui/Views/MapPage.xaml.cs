using food_market_narrator.Helpers;
using food_market_narrator.Services;

namespace food_market_narrator.Views;

[QueryProperty(nameof(Latitude), "lat")]
[QueryProperty(nameof(Longitude), "lng")]
[QueryProperty(nameof(LocationName), "name")]

public partial class MapPage : ContentPage
{
    private readonly POIService _poiService;
    private readonly ILocationService _locationService;
    private readonly TileServerService _tileServerService;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    public MapPage(
        POIService poiService,
        ILocationService locationService,
        TileServerService tileServerService)
    {
        InitializeComponent();
        _poiService = poiService;
        _locationService = locationService;
        _tileServerService = tileServerService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _locationService.LocationChanged += OnLocationChangedForMap;
        await _locationService.StartTrackingAsync();

        await MapHelper.LoadMapAsync(
        map,
        _poiService,
        _locationService,
        _tileServerService);
    }

    protected override void OnDisappearing()
    {
        _locationService.LocationChanged -= OnLocationChangedForMap;
        base.OnDisappearing();
    }

    private async void OnLocationChangedForMap(object? sender, Location location)
    {
        await map.UpdateUserLocationAsync(location.Latitude, location.Longitude);
        var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);
        _poiService.HighlightNearestPOI(map, nearest);
    }
}