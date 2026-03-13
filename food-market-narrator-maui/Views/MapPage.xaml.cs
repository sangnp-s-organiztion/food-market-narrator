using food_market_narrator.Helpers;
using food_market_narrator.Services;
using Mapsui.UI.Maui;

namespace food_market_narrator.Views;

[QueryProperty(nameof(Latitude), "lat")]
[QueryProperty(nameof(Longitude), "lng")]
[QueryProperty(nameof(LocationName), "name")]

public partial class MapPage : ContentPage
{
    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    public MapPage(
        IPOIService poiService,
        ILocationService locationService)
    {
        InitializeComponent();
        _poiService = poiService;
        _locationService = locationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _locationService.LocationChanged += OnLocationChangedForMap;
        await _locationService.StartTrackingAsync();

        await MapHelper.LoadMapAsync(
        mapControl,
        _poiService,
        _locationService);
    }

    protected override void OnDisappearing()
    {
        _locationService.LocationChanged -= OnLocationChangedForMap;
        base.OnDisappearing();
    }

    private void OnLocationChangedForMap(object? sender, Location location)
    {
        var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);
        MapHelper.HighlightPOI(mapControl, nearest);
    }
}