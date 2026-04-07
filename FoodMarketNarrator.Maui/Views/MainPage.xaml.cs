using food_market_narrator.Helpers;
using food_market_narrator.Models;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using System.Collections.Generic;
using System.Diagnostics;
using Mapsui;
using Mapsui.Projections;

namespace food_market_narrator.Views;

public partial class MainPage : ContentPage
{
    private static bool _hasAutoStartedNarrationThisSession;
    private static bool _hasAppliedStartupTrackingDelay;
    private static bool? _lastFloatingButtonVisibility;
    private const int FeaturedPoiPageSize = 10;

    // Khời tạo tọa độ và tên cho điểm
    private readonly IPOIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly ILocationService _locationService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IQrAccessService _qrAccessService;

    private bool _isInsidePOIUI = false; // trạng thái UI hiện tại có ở gần POI hay không
    private bool _isMapLoaded;
    private bool _isPoiListBound;
    private List<POI> _allPois = new();
    private int _currentPoiPageIndex;
    private Location? _lastKnownLocation;
    private bool _isInitializingMainPage;

    // private static bool _hasShownLanguagePopupThisSession;
    // private bool _languageSelected = Preferences.Get("language_selected", false);

	// Hàm khởi tạo MainPage mới
    public MainPage(
        IPOIService poiService,
        NarrationFlowService narrationFlowService,
        ILocationService locationService,
        IAudioLibraryService audioLibraryService,
        IQrAccessService qrAccessService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
        _audioLibraryService = audioLibraryService;
        _qrAccessService = qrAccessService;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        var sw = Stopwatch.StartNew();

        if (_lastFloatingButtonVisibility.HasValue)
        {
            var cachedVisibility = _lastFloatingButtonVisibility.Value;
            _isInsidePOIUI = cachedVisibility;
            FloatingButton.IsVisible = cachedVisibility;
        }

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _locationService.LocationChanged += OnLocationChangedForMap;
        LogPerf("OnAppearing: subscribed LocationChanged", sw);

        // Dời start tracking sau frame đầu để giảm giật lúc cold start.
        _ = StartTrackingDeferredAsync();

        // Ưu tiên dùng vị trí cache để cập nhật nút thuyết minh ngay khi quay lại MainPage.
        var currentLocation = _locationService.LastKnownLocation ?? _lastKnownLocation;
        if (currentLocation != null)
        {
            _lastKnownLocation = currentLocation;
            UpdateUIByLocation(currentLocation);
            _ = EnsurePoiDataReadyForUiAsync(currentLocation);
        }
        else
        {
            _ = PrimeUiWithLatestLocationAsync();
        }

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

        // Cập nhật text/disabled state của nút, trạng thái visible đã được quyết định ở nhánh trên.
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
                _allPois = poisData;
                _currentPoiPageIndex = 0;
                BindPoiPage();
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

    private async Task StartTrackingDeferredAsync()
    {
        try
        {
            if (!_hasAppliedStartupTrackingDelay)
            {
                _hasAppliedStartupTrackingDelay = true;
                await Task.Delay(AppSettings.StartupTrackingDelayMs);
            }

            await _locationService.StartTrackingAsync();
        }
        catch
        {
            // Ignore startup tracking failures to keep UI responsive.
        }
    }

    private async Task PrimeUiWithLatestLocationAsync()
    {
        try
        {
            var currentLocation = await _locationService.GetCurrentLocationAsync();
            if (currentLocation == null)
            {
                return;
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _lastKnownLocation = currentLocation;
                UpdateUIByLocation(currentLocation);
            });

            await EnsurePoiDataReadyForUiAsync(currentLocation);
        }
        catch
        {
            // Ignore transient location read errors; tracking loop will update UI later.
        }
    }

    private async Task EnsurePoiDataReadyForUiAsync(Location location)
    {
        try
        {
            await _poiService.GetAllPOIsAsync();
            MainThread.BeginInvokeOnMainThread(() => UpdateUIByLocation(location));
        }
        catch
        {
            // Ignore background preload failures; existing UI state stays usable.
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
        if (IsNarrationBlockedByQr())
        {
            await DisplayAlertAsync(
                "QR hết hạn",
                "Mã QR đã hết thời gian. Vui lòng quét lại QR để tiếp tục thuyết minh.",
                "OK");
            UpdateFloatingButtonUI();
            return;
        }

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

    private void OnZoomInTapped(object sender, TappedEventArgs e)
    {
        AdjustMapZoom(0.7);
    }

    private void OnZoomOutTapped(object sender, TappedEventArgs e)
    {
        AdjustMapZoom(1.3);
    }

    private async void OnMyLocationTapped(object sender, TappedEventArgs e)
    {
        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation == null)
        {
            return;
        }

        _lastKnownLocation = currentLocation;
        MapHelper.UpdateUserLocation(mapControl, currentLocation.Latitude, currentLocation.Longitude);
        CenterMapOn(currentLocation.Latitude, currentLocation.Longitude, 18);
    }

    private void AdjustMapZoom(double factor)
    {
        if (mapControl?.Map?.Navigator == null)
        {
            return;
        }

        var viewport = mapControl.Map.Navigator.Viewport;
        var minResolution = ToResolution(20);
        var maxResolution = ToResolution(3);
        var targetResolution = Math.Clamp(viewport.Resolution * factor, minResolution, maxResolution);
        var currentCenter = new MPoint(viewport.CenterX, viewport.CenterY);

        mapControl.Map.Navigator.CenterOnAndZoomTo(currentCenter, targetResolution);
        mapControl.Map.RefreshGraphics();
    }

    private void CenterMapOn(double latitude, double longitude, int zoomLevel)
    {
        if (mapControl?.Map?.Navigator == null)
        {
            return;
        }

        var spherical = SphericalMercator.FromLonLat(longitude, latitude);
        var center = new MPoint(spherical.x, spherical.y);
        mapControl.Map.Navigator.CenterOnAndZoomTo(center, ToResolution(zoomLevel));
        mapControl.Map.RefreshGraphics();
    }

    private static double ToResolution(int zoomLevel)
    {
        return 156543.03392 / Math.Pow(2, zoomLevel);
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

    private void OnPreviousPageClicked(object sender, EventArgs e)
    {
        if (_currentPoiPageIndex <= 0)
        {
            return;
        }

        _currentPoiPageIndex--;
        BindPoiPage();
    }

    private void OnNextPageClicked(object sender, EventArgs e)
    {
        var totalPages = GetTotalPoiPages();
        if (_currentPoiPageIndex >= totalPages - 1)
        {
            return;
        }

        _currentPoiPageIndex++;
        BindPoiPage();
    }

    private void BindPoiPage()
    {
        if (_allPois.Count == 0)
        {
            PoiList.ItemsSource = null;
            UpdatePaginationUi();
            return;
        }

        var pageItems = _allPois
            .Skip(_currentPoiPageIndex * FeaturedPoiPageSize)
            .Take(FeaturedPoiPageSize)
            .ToList();

        PoiList.ItemsSource = pageItems;
        UpdatePaginationUi();
    }

    private int GetTotalPoiPages()
    {
        if (_allPois.Count == 0)
        {
            return 1;
        }

        return (int)Math.Ceiling((double)_allPois.Count / FeaturedPoiPageSize);
    }

    private void UpdatePaginationUi()
    {
        var totalPages = GetTotalPoiPages();
        var currentPageDisplay = totalPages == 0 ? 0 : _currentPoiPageIndex + 1;

        PaginationContainer.IsVisible = _allPois.Count > FeaturedPoiPageSize;
        PageIndicatorLabel.Text = $"Trang {currentPageDisplay}/{totalPages}";

        var canGoPrevious = _currentPoiPageIndex > 0;
        var canGoNext = _currentPoiPageIndex < totalPages - 1;

        PreviousPageButton.IsEnabled = canGoPrevious;
        PreviousPageButton.Opacity = canGoPrevious ? 1 : 0.5;

        NextPageButton.IsEnabled = canGoNext;
        NextPageButton.Opacity = canGoNext ? 1 : 0.5;
    }

    // Cập nhật trạng thái ẩn/hiện của FloatingButton dựa trên khoảng cách đến POI gần nhất
    private void UpdateUIByLocation(Location location)
    {
        var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);

        var shouldShow = nearest != null
            && _poiService.GetDistanceMeters(location, nearest) <= AppSettings.TriggerDistanceMeters;

        _lastFloatingButtonVisibility = shouldShow;

        if (_isInsidePOIUI != shouldShow)
        {
            _isInsidePOIUI = shouldShow;
            FloatingButton.IsVisible = shouldShow;
        }

        UpdateFloatingButtonUI();

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

        var shouldHighlight = nearest != null
            && _poiService.GetDistanceMeters(location, nearest) < AppSettings.MapHighlightDistanceMeters;
        MapHelper.HighlightPOI(mapControl, shouldHighlight ? nearest : null);
    }

    // Cập nhật trạng thái của nút thuyết minh dựa trên trạng thái hiện tại của NarrationFlowService
    private void UpdateFloatingButtonUI()
    {
        var isBlocked = IsNarrationBlockedByQr();
        NarratorButton.IsEnabled = !isBlocked;
        FloatingButton.Opacity = isBlocked ? 0.55 : 1;

        if (isBlocked)
        {
            NarratorText.Text = "QR đã hết hạn - quét lại để thuyết minh";
            return;
        }

        if (_narrationFlowService.IsNarrating)
        {
            NarratorText.Text = "Dừng thuyết minh";
        }
        else
        {
            NarratorText.Text = "Bắt đầu thuyết minh tự động";
        }
    }

    private bool IsNarrationBlockedByQr()
    {
        if (!_qrAccessService.IsQrTimeRestricted)
        {
            return false;
        }

        var expiry = _qrAccessService.QrAccessExpiresAtUtc;
        return (expiry.HasValue && DateTime.UtcNow > expiry.Value)
            || string.Equals(_qrAccessService.LastBlockReason, "expired", StringComparison.OrdinalIgnoreCase);
    }
}
