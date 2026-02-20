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

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    public MapPage(
        POIService poiService,
        NarrationFlowService narrationFlowService)
    {
        InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        Location? initialLocation = null;
        if (Latitude != 0 && Longitude != 0)
        {
            initialLocation = new Location(Latitude, Longitude);
        }
        await MapHelper.LoadMap(map, _poiService, initialLocation);
    }
}