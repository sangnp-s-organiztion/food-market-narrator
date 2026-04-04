using food_market_narrator.Helpers;
using food_market_narrator.Models;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using System.Collections.Generic;
using System.Diagnostics;

namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
    private static bool _hasAutoStartedNarrationThisSession;

    // Khời tạo tọa độ và tên cho điểm
    private readonly IPOIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly ILocationService _locationService;
    private readonly IAudioLibraryService _audioLibraryService;

    private bool _isInsidePOIUI = false; // trạng thái UI hiện tại có ở gần POI hay không
    private bool _isMapLoaded;
    private bool _isPoiListBound;
    private Location? _lastKnownLocation;
    private bool _isInitializingMainPage;

    // private static bool _hasShownLanguagePopupThisSession;
    // private bool _languageSelected = Preferences.Get("language_selected", false);

	// Hàm khởi tạo MainPage mới
    public MainPage(
        IPOIService poiService,
        NarrationFlowService narrationFlowService,
        ILocationService locationService,
        IAudioLibraryService audioLibraryService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
        _audioLibraryService = audioLibraryService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var sw = Stopwatch.StartNew();

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _locationService.LocationChanged += OnLocationChangedForMap;
        LogPerf("OnAppearing: subscribed LocationChanged", sw);

        // Không chờ tracking để tránh block frame đầu khi vào trang.
        _ = _locationService.StartTrackingAsync();

        // Trả giao diện ngay, các phần nặng sẽ được tải nền.
        if (!_isInitializingMainPage)
        {
            _ = InitializeMainPageAsync();
        }

        // Chỉ tự bật 1 lần trong mỗi phiên chạy app (cold start).
        if (!_hasAutoStartedNarrationThisSession)
        {
            _narrationFlowService.StartNarration();
            _hasAutoStartedNarrationThisSession = true;
        }

        if (_audioLibraryService.ConsumeStartupOfflineNoticeFlag())
        {
            _ = DisplayAlert("Thông báo", "Vui lòng kết nối Internet để tải dữ liệu audio.", "OK");
        }

        // Cập nhật trạng thái UI dựa trên vị trí hiện tại
        var currentLocation = _lastKnownLocation;
        if (currentLocation != null)
        {
            UpdateUIByLocation(currentLocation);
        }
        UpdateFloatingButtonUI();
        LogPerf("OnAppearing: completed", sw);
    }

    private async Task InitializeMainPageAsync()
    {
        if (_isInitializingMainPage)
        {
            return;
        }

        _isInitializingMainPage = true;
        var sw = Stopwatch.StartNew();
        try
        {
            // Nhường 1 nhịp để UI kịp render frame đầu trước khi chạy tác vụ nặng.
            await Task.Yield();

            if (!_isMapLoaded)
            {
                await MapHelper.LoadMapAsync(mapControl, _poiService, _locationService, initialZoomLevel: 19);
                _isMapLoaded = true;
                LogPerf("Initialize: map loaded", sw);
            }

            if (!_isPoiListBound)
            {
                var poisData = await _poiService.GetAllPOIsAsync();
                PoiList.ItemsSource = poisData;
                _isPoiListBound = true;
                LogPerf($"Initialize: POI list bound ({poisData.Count})", sw);
            }

            if (_lastKnownLocation == null)
            {
                var currentLocation = await _locationService.GetCurrentLocationAsync();
                _lastKnownLocation = currentLocation;
                if (currentLocation != null)
                {
                    UpdateUIByLocation(currentLocation);
                }
                LogPerf("Initialize: first location acquired", sw);
            }

            LogPerf("Initialize: completed", sw);
        }
        finally
        {
            _isInitializingMainPage = false;
        }
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
            _lastKnownLocation = location;
            UpdateUIByLocation(location);
        });
    }

    private static void LogPerf(string message, Stopwatch sw)
    {
        // Console.WriteLine($"[Perf][MainPage] {message} at {sw.ElapsedMilliseconds} ms");
    }

    // Hàm xử lý khi nhấn nút bắt đầu thuyết minh
    private async void OnNarratorTapped(object sender, EventArgs e)
    {
        var animateTapTask = AnimateNarratorButtonTapAsync();

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

        await animateTapTask;
    }

    private async Task AnimateNarratorButtonTapAsync()
    {
        await FloatingButton.ScaleToAsync(0.93, 80, Easing.CubicOut);
        await FloatingButton.ScaleToAsync(1, 80, Easing.CubicIn);
    }

    // Hàm xử lý khi nhấn vào icon user (chuyển đến Settings)
    private async void OnUserIconTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync(nameof(SettingsPage));
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
        if (_isMapLoaded)
        {
            try
            {
                MapHelper.UpdateUserLocation(mapControl, location.Latitude, location.Longitude);
                MapHelper.CenterOnUserLocation(mapControl, location.Latitude, location.Longitude);
            }
            catch (Exception)
            {
                // Ignore transient map-camera errors while map is attaching/re-rendering.
            }
        }

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
            NarratorText.Text = "Bắt đầu thuyết minh tự động";
        }
    }
}
