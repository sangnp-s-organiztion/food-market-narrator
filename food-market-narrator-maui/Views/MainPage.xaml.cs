using food_market_narrator.Helpers;
using food_market_narrator.Models;
using food_market_narrator.Services;
using System.Collections.Generic;

namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
    // Khời tạo tọa độ và tên cho điểm
    private readonly POIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly ILocationService _locationService;
    private readonly TileServerService _tileServerService;
    private readonly LanguageService _languageService = new();

    private readonly Dictionary<string, Border> _languageOptions;
    private readonly Dictionary<string, Label> _languageChecks;
    private bool _isInsidePOIUI = false; // trạng thái UI hiện tại

    private bool _isFirstLoad = true;

	// Hàm khởi tạo MainPage mới
	public MainPage(POIService poiService, NarrationFlowService narrationFlowService, ILocationService locationService, TileServerService tileServerService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
        _tileServerService = tileServerService;

        _languageOptions = new Dictionary<string, Border>
        {
            ["vi-VN"] = VietnameseOption,
            ["en-US"] = EnglishOption,
            ["zh-CN"] = ChineseOption,
            ["ko-KR"] = KoreanOption,
            ["ja-JP"] = JapaneseOption
        };

        _languageChecks = new Dictionary<string, Label>
        {
            ["vi-VN"] = VietnameseCheck,
            ["en-US"] = EnglishCheck,
            ["zh-CN"] = ChineseCheck,
            ["ko-KR"] = KoreanCheck,
            ["ja-JP"] = JapaneseCheck
        };

        ApplyLanguageSelectionStyle(_languageService.CurrentLanguage);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _locationService.LocationChanged += OnLocationChangedForMap;
        await _locationService.StartTrackingAsync();

        // _narrationFlowService.StartNarration();

        // Load map data on appearing, reusing helper logic
        await MapHelper.LoadMapAsync(map, _poiService, _locationService, _tileServerService);

        // Hiện popup chọn ngôn ngữ khi mới vào app
        Console.WriteLine("Is First Load: " + _isFirstLoad);
        if (_isFirstLoad)
        {
            _isFirstLoad = false;
            await Task.Delay(300);
            OnLanguageButtonTapped(this, EventArgs.Empty); // Tự động mở popup chọn ngôn ngữ
        }

        // Hiển thị POI lên giao diện
        Console.WriteLine("Loading POIs for display...");
        Console.WriteLine("The number of POIs loaded: " + (_poiService.GetAllPOIsAsync().Result.Count));
        var poisData = await _poiService.GetPOIsAsync();
        PoiList.ItemsSource = poisData;      

        // Cập nhật trạng thái UI dựa trên vị trí hiện tại
        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation != null)
        {
            UpdateUIByLocation(currentLocation);
        }
        UpdateFloatingButtonUI();       
    }

    protected override void OnDisappearing()
    {
        _locationService.LocationChanged -= OnLocationChangedForMap;
        base.OnDisappearing();
    }

    // Hàm xử lý khi thay đổi vị trí để cập nhật giao diện và thuyết minh
    private async void OnLocationChangedForMap(object? sender, Location location)
    {
        await map.UpdateUserLocationAsync(location.Latitude, location.Longitude);
        var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);
        _poiService.HighlightNearestPOI(map, nearest);
    
        UpdateUIByLocation(location);
    }

    // Hàm xử lý khi nhấn nút bắt đầu thuyết minh
    private async void OnNarratorTapped(object sender, EventArgs e)
    {
        // Hiệu ứng nhấn xuống
        await FloatingButton.ScaleToAsync(0.93, 80, Easing.CubicOut);
        await FloatingButton.ScaleToAsync(1, 80, Easing.CubicIn);

        if (!_narrationFlowService.IsNarrating)
        {
            _narrationFlowService.StartNarration();
            // await _narrationFlowService.SmartPlayAsync();
        }
        else
        {
            // dừng thuyet minh
            _narrationFlowService.StopNarration();

        }
        // cập nhật lại UI của nút thuyết minh
        UpdateFloatingButtonUI();
    }

    // Hàm xử lý khi nhấn nút chọn ngôn ngữ
    private async void OnLanguageButtonTapped(object sender, EventArgs e)
    {
        ApplyLanguageSelectionStyle(_languageService.CurrentLanguage);
        LanguagePopupOverlay.IsVisible = true;
        await LanguagePopupOverlay.FadeToAsync(1, 180, Easing.CubicOut);
    }

    // Hàm xử lý khi nhấn nút đóng popup ngôn ngữ
    private async void OnLanguageCloseTapped(object sender, EventArgs e)
    {
        await HideLanguagePopupAsync();
    }

    // Hàm xử lý khi nhấn vào vùng phủ ngôn ngữ (để đóng popup) 
    private async void OnLanguageOverlayTapped(object sender, EventArgs e)
    {
        await HideLanguagePopupAsync();
    }

    // Hàm xử lý khi nhấn vào một tùy chọn ngôn ngữ
    private async void OnLanguageOptionTapped(object sender, TappedEventArgs e)
    {
        if (e.Parameter is not string cultureCode)
        {
            return;
        }

        ApplyLanguageSelectionStyle(cultureCode);
        await HideLanguagePopupAsync();

        if (_languageService.CurrentLanguage != cultureCode)
        {
            _languageService.ChangeLanguage(cultureCode);
        }

        // phát audio sau khi đổi ngôn ngữ
        _narrationFlowService.StartNarration();
        UpdateFloatingButtonUI();
    }

    // Hàm ẩn popup ngôn ngữ với hiệu ứng mờ dần
    private async Task HideLanguagePopupAsync()
    {
        await LanguagePopupOverlay.FadeToAsync(0, 140, Easing.CubicIn);
        LanguagePopupOverlay.IsVisible = false;
    }

    // Hàm áp dụng style cho tùy chọn ngôn ngữ được chọn
    private void ApplyLanguageSelectionStyle(string cultureCode)
    {
        foreach (var option in _languageOptions)
        {
            var isSelected = option.Key == cultureCode;

            option.Value.BackgroundColor = isSelected
                ? Color.FromArgb("#F7F3EF")
                : Color.FromArgb("#F1EDE9");

            option.Value.StrokeThickness = isSelected ? 1.5 : 0;
            option.Value.Stroke = isSelected
                ? Color.FromArgb("#F48C06")
                : Colors.Transparent;

            _languageChecks[option.Key].IsVisible = isSelected;
        }
    }

    // Hàm xử lý khi nhấn vào một POI trong danh sách để hiển thị chi tiết
    private async void OnPoiDetailTapped(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not POI selectedPoi)
        {
            return;
        }

        PoiList.SelectedItem = null;

        if (string.IsNullOrWhiteSpace(selectedPoi.restaurantId))
        {
            return;
        }

        var encodedId = Uri.EscapeDataString(selectedPoi.restaurantId);
        await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?restaurantId={encodedId}");
    }

    // Cập nhật trạng thái ẩn/hiện của FloatingButton dựa trên khoảng cách đến POI gần nhất
    private void UpdateUIByLocation(Location location)
    {
        var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);

        _poiService.HighlightNearestPOI(map, nearest);

        bool shouldShow = false;

        if (nearest != null)
        {
            var distance = Location.CalculateDistance(
                location,
                new Location(nearest.Latitude, nearest.Longitude),
                DistanceUnits.Kilometers) * 1000;

            shouldShow = distance <= 30;
        }

        if (_isInsidePOIUI != shouldShow)
        {
            _isInsidePOIUI = shouldShow;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                FloatingButton.IsVisible = shouldShow;
            });
        }
    }

    // Cập nhật trạng thái của nút thuyết minh dựa trên trạng thái hiện tại của NarrationFlowService
    private void UpdateFloatingButtonUI()
    {
        if (_narrationFlowService.IsNarrating)
        {
            NarratorText.Text = "Dừng thuyết minh";
        }
        else
        {
            NarratorText.Text = "Bắt đầu thuyết minh";
        }
    }
}