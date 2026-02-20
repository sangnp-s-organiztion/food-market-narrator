using food_market_narrator.Helpers;
using food_market_narrator.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;

namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
    // Khời tạo tọa độ và tên cho điểm
    private readonly POIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    private bool _isFirstLoad = true;

	// Hàm khởi tạo MainPage mới
	public MainPage(POIService poiService, NarrationFlowService narrationFlowService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        
        Loaded += OnMapLoadedAndFocused;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load map data on appearing, reusing helper logic
        await MapHelper.LoadMap(map, _poiService);
    }

    private async void OnMapLoadedAndFocused(object sender, EventArgs e)
    {
       if (_isFirstLoad)
       {
           await MapHelper.LoadMap(map, _poiService);
           _isFirstLoad = false;
       }
    }
}