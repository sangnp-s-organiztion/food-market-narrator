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
    private enum PoiCategoryFilter
    {
        All,
        Nearby,
        Favorite,
        OpenNow
    }

    private static bool _hasAutoStartedNarrationThisSession;
    private static bool _hasAppliedStartupTrackingDelay;
    private static bool? _lastFloatingButtonVisibility;
    private const int FeaturedPoiPageSize = 10;
    private const double NearbyFilterRadiusMeters = 100;

    // Khời tạo tọa độ và tên cho điểm
    private readonly IPOIService _poiService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly ILocationService _locationService;
    private readonly IAudioLibraryService _audioLibraryService;
    private readonly IFavoriteService _favoriteService;

    private bool _isInsidePOIUI = false; // trạng thái UI hiện tại có ở gần POI hay không
    private bool _isMapLoaded;
    private bool _isPoiListBound;
    private List<POI> _allPois = new();
    private int _currentPoiPageIndex;
    private Location? _lastKnownLocation;
    private bool _isInitializingMainPage;
    private PoiCategoryFilter _activePoiFilter = PoiCategoryFilter.All;
    private List<POI> _filteredPois = new();

    // private static bool _hasShownLanguagePopupThisSession;
    // private bool _languageSelected = Preferences.Get("language_selected", false);

	// Hàm khởi tạo MainPage mới
    public MainPage(
        IPOIService poiService,
        NarrationFlowService narrationFlowService,
        ILocationService locationService,
        IAudioLibraryService audioLibraryService,
        IFavoriteService favoriteService)
	{
		InitializeComponent();
        _poiService = poiService;
        _narrationFlowService = narrationFlowService;
        _locationService = locationService;
        _audioLibraryService = audioLibraryService;
        _favoriteService = favoriteService;
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
            _ = DisplayAlert("Thông báo", "Vui lòng kết nối Internet để tải dữ liệu audio.", "Đóng");
        }

        // Cập nhật text/disabled state của nút, trạng thái visible đã được quyết định ở nhánh trên.
        UpdateFloatingButtonUI();

        if (_isPoiListBound)
        {
            ApplyPoiFilterAndRefresh();
        }

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
                _isPoiListBound = true;
                _currentPoiPageIndex = 0;
                ApplyPoiFilterAndRefresh();
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

    private void OnFilterAllTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.All);
    }

    private async void OnFilterNearTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.Nearby);
        await RefreshNearbyFilterWithCurrentLocationAsync();
    }

    private void OnFilterFavoriteTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.Favorite);
    }

    private void OnFilterOpenTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.OpenNow);
    }

    private void SetPoiFilter(PoiCategoryFilter filter)
    {
        _activePoiFilter = filter;
        _currentPoiPageIndex = 0;
        ApplyPoiFilterAndRefresh();
    }

    private async Task RefreshNearbyFilterWithCurrentLocationAsync()
    {
        var location = _lastKnownLocation ?? _locationService.LastKnownLocation;
        if (location == null)
        {
            location = await _locationService.GetCurrentLocationAsync();
        }

        if (location == null || _activePoiFilter != PoiCategoryFilter.Nearby)
        {
            return;
        }

        _lastKnownLocation = location;
        ApplyPoiFilterAndRefresh(location);
    }

    private void ApplyPoiFilterAndRefresh(Location? referenceLocation = null)
    {
        var location = referenceLocation ?? _lastKnownLocation ?? _locationService.LastKnownLocation;
        _filteredPois = GetFilteredPois(location);

        var totalPages = GetTotalPoiPages();
        if (_currentPoiPageIndex >= totalPages)
        {
            _currentPoiPageIndex = Math.Max(0, totalPages - 1);
        }

        BindPoiPage();
        UpdateCategoryChipUi();
        UpdateMapHighlightByCurrentFilter(location);
    }

    private List<POI> GetFilteredPois(Location? location)
    {
        if (_allPois.Count == 0)
        {
            return new List<POI>();
        }

        IEnumerable<POI> query = _allPois;
        switch (_activePoiFilter)
        {
            case PoiCategoryFilter.Nearby:
                if (location == null)
                {
                    return new List<POI>();
                }

                query = query.Where(p => _poiService.GetDistanceMeters(location, p) <= NearbyFilterRadiusMeters);
                break;

            case PoiCategoryFilter.Favorite:
                var favoriteIds = _favoriteService
                    .GetFavorites()
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                query = query.Where(p => !string.IsNullOrWhiteSpace(p.restaurantId) && favoriteIds.Contains(p.restaurantId));
                break;

            case PoiCategoryFilter.OpenNow:
                query = query.Where(p => p.IsCurrentlyOpen);
                break;

            case PoiCategoryFilter.All:
            default:
                break;
        }

        return query.ToList();
    }

    private IEnumerable<string>? GetVisiblePoiIdsForCurrentFilter()
    {
        if (_activePoiFilter == PoiCategoryFilter.All)
        {
            return null;
        }

        return _filteredPois
            .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId))
            .Select(p => p.restaurantId)
            .ToList();
    }

    private void UpdateMapHighlightByCurrentFilter(Location? location)
    {
        if (!_isMapLoaded)
        {
            return;
        }

        var highlightCandidates = _activePoiFilter == PoiCategoryFilter.All
            ? _allPois
            : _filteredPois;

        var nearest = location == null
            ? null
            : _poiService.GetNearestPOI(location, highlightCandidates);

        var shouldHighlight = location != null
            && nearest != null
            && _poiService.GetDistanceMeters(location, nearest) < AppSettings.MapHighlightDistanceMeters;

        MapHelper.HighlightPOI(
            mapControl,
            shouldHighlight ? nearest : null,
            visiblePoiIds: GetVisiblePoiIdsForCurrentFilter());
    }

    private void UpdateCategoryChipUi()
    {
        SetChipState(MainFilterAllChip, MainFilterAllLabel, _activePoiFilter == PoiCategoryFilter.All);
        SetChipState(MainFilterNearChip, MainFilterNearLabel, _activePoiFilter == PoiCategoryFilter.Nearby, MainFilterNearIcon);
        SetChipState(MainFilterFavoriteChip, MainFilterFavoriteLabel, _activePoiFilter == PoiCategoryFilter.Favorite, MainFilterFavoriteIcon);
        SetChipState(MainFilterOpenChip, MainFilterOpenLabel, _activePoiFilter == PoiCategoryFilter.OpenNow, MainFilterOpenIcon);
    }

    private static void SetChipState(Border border, Label textLabel, bool isActive, Label? iconLabel = null)
    {
        border.BackgroundColor = isActive ? Color.FromArgb("#F48C06") : Color.FromArgb("#F5F1EE");
        textLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#3E2723");
        if (iconLabel != null)
        {
            iconLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#3E2723");
        }
    }

    private void BindPoiPage()
    {
        if (_filteredPois.Count == 0)
        {
            PoiList.ItemsSource = new List<POI>();
            UpdatePaginationUi();
            return;
        }

        var pageItems = _filteredPois
            .Skip(_currentPoiPageIndex * FeaturedPoiPageSize)
            .Take(FeaturedPoiPageSize)
            .ToList();

        PoiList.ItemsSource = pageItems;
        UpdatePaginationUi();
    }

    private int GetTotalPoiPages()
    {
        if (_filteredPois.Count == 0)
        {
            return 1;
        }

        return (int)Math.Ceiling((double)_filteredPois.Count / FeaturedPoiPageSize);
    }

    private void UpdatePaginationUi()
    {
        var totalPages = GetTotalPoiPages();
        var currentPageDisplay = totalPages == 0 ? 0 : _currentPoiPageIndex + 1;

        PaginationContainer.IsVisible = _filteredPois.Count > FeaturedPoiPageSize;
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

        if (_isPoiListBound && _activePoiFilter == PoiCategoryFilter.Nearby)
        {
            ApplyPoiFilterAndRefresh(location);
            return;
        }

        UpdateMapHighlightByCurrentFilter(location);
    }

    // Cập nhật trạng thái của nút thuyết minh dựa trên trạng thái hiện tại của NarrationFlowService
    private void UpdateFloatingButtonUI()
    {
        NarratorButton.IsEnabled = true;
        FloatingButton.Opacity = 1;

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
