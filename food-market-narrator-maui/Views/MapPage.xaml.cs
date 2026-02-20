using food_market_narrator.Helpers;
using food_market_narrator.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace food_market_narrator.Views;

[QueryProperty(nameof(Latitude), "lat")]
[QueryProperty(nameof(Longitude), "lng")]
[QueryProperty(nameof(LocationName), "name")]

public partial class MapPage : ContentPage
{
    private readonly POIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly ILocationService _locationService;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    public MapPage(
        POIService poiService,
        NarrationFlowService narrationFlowService,
        ILocationService locationService)
    {
        InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await MapHelper.LoadMapAsync(
        map,
        _poiService,
        _locationService);
    }
}