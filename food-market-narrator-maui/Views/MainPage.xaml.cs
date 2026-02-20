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
    private readonly ILocationService _locationService;


    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    private bool _isFirstLoad = true;

	// Hàm khởi tạo MainPage mới
	public MainPage(POIService poiService, NarrationFlowService narrationFlowService, ILocationService locationService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Load map data on appearing, reusing helper logic
        await MapHelper.LoadMapAsync(map, _poiService, _locationService);
    }

    public async void CheckAndNarrateAsync(object sender, EventArgs e)
    {
        //// 1. Lấy vị trí hiện tại
        //var currentLocation = await _locationService.GetCurrentLocationAsync();
        
        //// 2. Gọi hàm check và narrate với FORCE = TRUE (bỏ qua check khoảng cách)
        //await _narrationFlowService.CheckAndNarrateAsync(currentLocation, force: true);
    }

    private async void OnNarratorTapped(object sender, EventArgs e)
    {
        // Hiệu ứng nhấn xuống
        await FloatingButton.ScaleTo(0.93, 80, Easing.CubicOut);
        await FloatingButton.ScaleTo(1, 80, Easing.CubicIn);

        _narrationFlowService.StartNarration();
    }
}