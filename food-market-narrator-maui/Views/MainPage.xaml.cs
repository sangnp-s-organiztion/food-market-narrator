using food_market_narrator.Helpers;
using food_market_narrator.Models;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using System.Collections.Generic;
using Microsoft.Maui.Controls.Shapes;

namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
    private static bool _hasAutoStartedNarrationThisSession;

    // Khời tạo tọa độ và tên cho điểm
    private readonly IPOIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly ILocationService _locationService;
    private readonly ILanguageService _languageService;

    private readonly Dictionary<string, Border> _languageOptions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Label> _languageChecks = new(StringComparer.OrdinalIgnoreCase);
    private bool _isLanguageOptionsLoaded;
    private bool _isInsidePOIUI = false; // trạng thái UI hiện tại có ở gần POI hay không

    // private static bool _hasShownLanguagePopupThisSession;
    // private bool _languageSelected = Preferences.Get("language_selected", false);

	// Hàm khởi tạo MainPage mới
    public MainPage(
        IPOIService poiService,
        NarrationFlowService narrationFlowService,
        ILocationService locationService,
        ILanguageService languageService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
        _languageService = languageService;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        await EnsureLanguageOptionsLoadedAsync();

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _locationService.LocationChanged += OnLocationChangedForMap;

        await Task.Delay(500); // nhỏ thôi, chờ UI ready
        await _locationService.StartTrackingAsync();

        // _narrationFlowService.StartNarration();

        // Load map data on appearing, reusing helper logic
        await MapHelper.LoadMapAsync(mapControl, _poiService, _locationService, initialZoomLevel: 19); // zoom level cao hơn để tập trung vào khu vực chợ
        // Hiện popup chọn ngôn ngữ khi mới vào app
        bool languageSelected = Preferences.Get("language_selected", false);
        Console.WriteLine("Language selected: " + languageSelected);
        if (!languageSelected)
        {
            // _hasShownLanguagePopupThisSession = true;
            await Task.Delay(300);
            OnLanguageButtonTapped(this, EventArgs.Empty); // Tự động mở popup chọn ngôn ngữ
        }
        else
        {
            // Chỉ tự bật 1 lần trong mỗi phiên chạy app (cold start).
            if (!_hasAutoStartedNarrationThisSession)
            {
                _narrationFlowService.StartNarration();
                _hasAutoStartedNarrationThisSession = true;
            }
        }

        // Hiển thị POI lên giao diện
        Console.WriteLine("Loading POIs for display...");
        
        var allPois = await _poiService.GetAllPOIsAsync();
        Console.WriteLine("The number of POIs loaded: " + allPois.Count);

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
    private void OnLocationChangedForMap(object? sender, Location location)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            UpdateUIByLocation(location);
        });
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
        await EnsureLanguageOptionsLoadedAsync();
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

    // Hàm đảm bảo rằng các tùy chọn ngôn ngữ đã được tải và hiển thị trong popup
    private async Task EnsureLanguageOptionsLoadedAsync()
    {
        if (_isLanguageOptionsLoaded)
        {
            return;
        }

        var languages = await _languageService.GetAllLanguagesAsync();
        if (languages.Count == 0)
        {
            languages = new List<LanguageModel>
            {
                new() { LanguageCode = _languageService.CurrentLanguage, LanguageName = _languageService.CurrentLanguage }
            };
        }

        BuildLanguageOptions(languages);
        _isLanguageOptionsLoaded = true;
        ApplyLanguageSelectionStyle(_languageService.CurrentLanguage);
    }

    // Hàm xây dựng giao diện cho các tùy chọn ngôn ngữ trong popup
    private void BuildLanguageOptions(IEnumerable<LanguageModel> languages)
    {
        LanguageOptionsContainer.Children.Clear();
        _languageOptions.Clear();
        _languageChecks.Clear();

        foreach (var language in languages)
        {
            if (string.IsNullOrWhiteSpace(language.LanguageCode))
            {
                continue;
            }

            var optionBorder = new Border
            {
                BackgroundColor = Color.FromArgb("#F1EDE9"),
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Padding = new Thickness(14, 12)
            };

            optionBorder.GestureRecognizers.Add(new TapGestureRecognizer
            {
                CommandParameter = language.LanguageCode,
                Command = new Command(async () => await OnLanguageOptionTappedAsync(language.LanguageCode))
            });

            var row = new Grid
            {
                ColumnDefinitions =
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                VerticalOptions = LayoutOptions.Center
            };

            row.Add(new Label
            {
                Text = GetFlagByLanguageCode(language.LanguageCode),
                FontSize = 20,
                VerticalOptions = LayoutOptions.Center
            });

            row.Add(new Label
            {
                Text = language.LanguageName,
                FontSize = 20,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#1A1A1A"),
                VerticalOptions = LayoutOptions.Center
            }, 1, 0);

            var checkLabel = new Label
            {
                Text = "✓",
                FontSize = 16,
                TextColor = Color.FromArgb("#F48C06"),
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                IsVisible = false
            };
            row.Add(checkLabel, 2, 0);

            optionBorder.Content = row;

            _languageOptions[language.LanguageCode] = optionBorder;
            _languageChecks[language.LanguageCode] = checkLabel;
            LanguageOptionsContainer.Children.Add(optionBorder);
        }
    }

    // Hàm xử lý khi người dùng chọn một ngôn ngữ từ popup
    private async Task OnLanguageOptionTappedAsync(string cultureCode)
    {
        if (string.IsNullOrWhiteSpace(cultureCode))
        {
            return;
        }

        ApplyLanguageSelectionStyle(cultureCode);
        await HideLanguagePopupAsync();

        Preferences.Set("language_selected", true);
        Preferences.Set("language", cultureCode);

        if (_languageService.CurrentLanguage != cultureCode)
        {
            _languageService.ChangeLanguage(cultureCode);
        }

        _narrationFlowService.StartNarration();
        UpdateFloatingButtonUI();
    }
    
    // Hàm lấy biểu tượng cờ dựa trên mã ngôn ngữ
    private string GetFlagByLanguageCode(string languageCode)
    {
        return languageCode.ToLowerInvariant() switch
        {
            "vi-vn" => "🇻🇳",
            "en-us" => "🇺🇸",
            "zh-cn" => "🇨🇳",
            "ko-kr" => "🇰🇷",
            "ja-jp" => "🇯🇵",
            _ => "🌐"
        };
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
        MapHelper.UpdateUserLocation(mapControl, location.Latitude, location.Longitude);

        var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);

        var shouldHighlight = nearest != null
            && _poiService.GetDistanceMeters(location, nearest) < AppSettings.MapHighlightDistanceMeters;
        MapHelper.HighlightPOI(mapControl, shouldHighlight ? nearest : null);

        bool shouldShow = false;

        if (nearest != null)
        {
            var distance = _poiService.GetDistanceMeters(location, nearest);

            shouldShow = distance <= AppSettings.TriggerDistanceMeters;
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