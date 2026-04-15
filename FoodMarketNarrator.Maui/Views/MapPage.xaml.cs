using food_market_narrator.Helpers;
using food_market_narrator.Models;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using Microsoft.Maui.Networking;
using System.Globalization;
using System.Text;

namespace food_market_narrator.Views;

[QueryProperty(nameof(Latitude), "lat")]
[QueryProperty(nameof(Longitude), "lng")]
[QueryProperty(nameof(LocationName), "name")]
[QueryProperty(nameof(TourPoiIds), "tourPoiIds")]
[QueryProperty(nameof(TourName), "tourName")]
[QueryProperty(nameof(TourStopOrders), "tourStopOrders")]

public partial class MapPage : ContentPage
{
    private enum PoiCategoryFilter
    {
        All,
        Nearby,
        Favorite,
        OpenNow
    }

    private const double MarkerTapPixelRadius = 28;
    private const double NearbyFilterRadiusMeters = 100;
    private const int DefaultZoomLevel = 16;
    private const int MinZoomLevel = 3;
    private const int MaxZoomLevel = 20;
    private const int MyLocationZoomLevel = 18;

    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private readonly NarrationFlowService _narrationFlowService;
    private readonly IFavoriteService _favoriteService;
    private List<POI> _pois = new();
    private List<POI> _searchSuggestions = new();
    private POI? _selectedPoi;
    private HashSet<string> _searchHighlightedPoiIds = new(StringComparer.Ordinal);
    private HashSet<string> _tourPoiFilterIds = new(StringComparer.Ordinal);
    private Dictionary<string, int> _tourStopOrdersByPoiId = new(StringComparer.Ordinal);
    private string? _activeTourName;
    private string? _tourPoiIdsRaw;
    private string? _tourStopOrdersRaw;
    private Location? _lastKnownLocation;
    private bool _isMapLoaded;
    private bool _hasShownOfflineMapUnavailableNotice;
    private CancellationTokenSource? _searchDebounceCts;
    private PoiCategoryFilter _activePoiFilter = PoiCategoryFilter.All;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    public string? TourPoiIds
    {
        get => _tourPoiIdsRaw;
        set
        {
            _tourPoiIdsRaw = value;
            _tourPoiFilterIds = ParsePoiIdSet(value);
            ApplyPoiModeFromState(focusOnTour: false);
        }
    }

    public string? TourName
    {
        get => _activeTourName;
        set
        {
            _activeTourName = DecodeQueryValue(value);
            UpdateTourFilterBanner();
        }
    }

    public string? TourStopOrders
    {
        get => _tourStopOrdersRaw;
        set
        {
            _tourStopOrdersRaw = value;
            _tourStopOrdersByPoiId = ParsePoiStopOrderMap(value);
            ApplyPoiModeFromState(focusOnTour: false);
        }
    }

    public MapPage(
        IPOIService poiService,
        ILocationService locationService,
        NarrationFlowService narrationFlowService,
        IFavoriteService favoriteService)
    {
        InitializeComponent();
        _poiService = poiService;
        _locationService = locationService;
        _narrationFlowService = narrationFlowService;
        _favoriteService = favoriteService;
    }

    // Khi trang xuất hiện, bắt đầu theo dõi vị trí và tải dữ liệu bản đồ
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Hủy đăng ký hàm xử lí sự kiện thay đổi vị trí ng dùng để tránh bị đăng ký nhiều lần nếu người dùng vào ra trang nhiều lần
        _locationService.LocationChanged -= OnLocationChangedForMap;

        // Đăng ký lại hàm xử lí sự kiện thay đổi vị trí ng dùng để cập nhật bản đồ khi vị trí thay đổi
        _locationService.LocationChanged += OnLocationChangedForMap;
        _ = _locationService.StartTrackingAsync();

        if (!_isMapLoaded)
        {
            // Chỉ tải map/layer nặng một lần cho mỗi instance page.
            await MapHelper.LoadMapAsync(
                mapControl,
                _poiService,
                _locationService,
                initialZoomLevel: DefaultZoomLevel);
            _isMapLoaded = true;
        }

        await ShowOfflineMapNoticeIfNeededAsync();

        if (_pois.Count == 0)
        {
            _pois = await _poiService.GetAllPOIsAsync();
        }

        mapControl.MapTapped -= OnMapTapped;
        mapControl.MapTapped += OnMapTapped;

        SearchClearButton.IsVisible = !string.IsNullOrWhiteSpace(SearchEntry.Text);
        SearchSuggestionsContainer.IsVisible = false;
        _lastKnownLocation = _locationService.LastKnownLocation;
        UpdateCategoryFilterUi();
        ApplyNarrationScopeFromTourFilter();

        HideSelectedPoiCard();
        ApplyPoiModeFromState(focusOnTour: true);
    }

    protected override void OnDisappearing()
    {
        mapControl.MapTapped -= OnMapTapped;

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _searchDebounceCts?.Cancel();
        _narrationFlowService.ClearAutoNarrationPoiScope();
        base.OnDisappearing();
    }

    // cập nhật vị trí trên bản đồ khi vị trí ng dùng changed và kiểm tra xem có POI nào gần đó để 2light không
    private void OnLocationChangedForMap(object? sender, Location location)
    {
        // Cập nhật vị trí người dùng trên bản đồ và kiểm tra POI gần nhất
        MainThread.BeginInvokeOnMainThread(() =>
        {
            _lastKnownLocation = location;
            MapHelper.UpdateUserLocation(mapControl, location.Latitude, location.Longitude);

            if (_searchHighlightedPoiIds.Count > 0
                || _tourPoiFilterIds.Count > 0
                || _activePoiFilter != PoiCategoryFilter.All)
            {
                ApplyPoiModeFromState(focusOnTour: false);
                return;
            }

            var nearest = _poiService.GetNearestPOI(location.Latitude, location.Longitude);
            var shouldHighlight = nearest != null
                && _poiService.GetDistanceMeters(location, nearest) < AppSettings.MapHighlightDistanceMeters;
            MapHelper.HighlightPOI(mapControl, shouldHighlight ? nearest : null);
        });
    }

    private void OnZoomInTapped(object sender, TappedEventArgs e)
    {
        AdjustZoom(0.7);
    }

    private void OnZoomOutTapped(object sender, TappedEventArgs e)
    {
        AdjustZoom(1.3);
    }

    private async void OnMyLocationTapped(object sender, TappedEventArgs e)
    {
        var currentLocation = await _locationService.GetCurrentLocationAsync();
        if (currentLocation == null)
        {
            await DisplayAlertAsync("Thông báo", "Không thể lấy vị trí hiện tại.", "Đóng");
            return;
        }

    _lastKnownLocation = currentLocation;
        CenterMapOn(currentLocation.Latitude, currentLocation.Longitude, MyLocationZoomLevel);
        MapHelper.UpdateUserLocation(mapControl, currentLocation.Latitude, currentLocation.Longitude);
    ApplyPoiModeFromState(focusOnTour: false);
    }

    private void AdjustZoom(double factor)
    {
        if (mapControl?.Map?.Navigator == null)
        {
            return;
        }

        var viewport = mapControl.Map.Navigator.Viewport;

        var minResolution = ToResolution(MaxZoomLevel);
        var maxResolution = ToResolution(MinZoomLevel);
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

        var clampedZoom = Math.Clamp(zoomLevel, MinZoomLevel, MaxZoomLevel);
        var spherical = SphericalMercator.FromLonLat(longitude, latitude);
        var center = new MPoint(spherical.x, spherical.y);

        mapControl.Map.Navigator.CenterOnAndZoomTo(center, ToResolution(clampedZoom));
        mapControl.Map.RefreshGraphics();
    }

    private static double ToResolution(int zoomLevel)
    {
        return 156543.03392 / Math.Pow(2, zoomLevel);
    }

    private void OnMapTapped(object? sender, MapEventArgs e)
    {
        _searchHighlightedPoiIds.Clear();
        SearchSuggestionsContainer.IsVisible = false;

        var interactivePois = GetInteractivePois();

        if (interactivePois.Count == 0)
        {
            HideSelectedPoiCard();
            ApplyPoiModeFromState(focusOnTour: false);
            return;
        }

        if (e.WorldPosition == null)
        {
            HideSelectedPoiCard();
            ApplyPoiModeFromState(focusOnTour: false);
            return;
        }

        var tapLonLat = SphericalMercator.ToLonLat(e.WorldPosition.X, e.WorldPosition.Y);
        var tappedLocation = new Location(tapLonLat.lat, tapLonLat.lon);
        var nearestPoi = _poiService.GetNearestPOI(tappedLocation, interactivePois);

        if (nearestPoi == null)
        {
            HideSelectedPoiCard();
            ApplyPoiModeFromState(focusOnTour: false);
            return;
        }

        var viewportResolution = mapControl.Map?.Navigator?.Viewport.Resolution ?? ToResolution(DefaultZoomLevel);
        var tapThresholdMeters = Math.Clamp(viewportResolution * MarkerTapPixelRadius, 12, 150);
        var distanceMeters = _poiService.GetDistanceMeters(tappedLocation, nearestPoi);

        if (distanceMeters > tapThresholdMeters)
        {
            HideSelectedPoiCard();
            ApplyPoiModeFromState(focusOnTour: false);
            return;
        }

        ShowSelectedPoiCard(nearestPoi);

        if (_tourPoiFilterIds.Count > 0)
        {
            ApplyPoiModeFromState(focusOnTour: false);
            return;
        }

        MapHelper.HighlightPOI(mapControl, nearestPoi);
    }

    private async void OnSearchSubmitted(object? sender, EventArgs e)
    {
        var keyword = SearchEntry.Text?.Trim();
        SearchSuggestionsContainer.IsVisible = false;

        if (string.IsNullOrWhiteSpace(keyword))
        {
            ClearSearchState();
            return;
        }

        if (_pois.Count == 0)
        {
            _pois = await _poiService.GetAllPOIsAsync();
        }

        var searchablePois = GetInteractivePois();

        var matchedPois = FindMatchingPois(keyword, maxResults: searchablePois.Count, sourcePois: searchablePois);
        if (matchedPois.Count == 0)
        {
            ClearSearchState();
            await DisplayAlertAsync("Không tìm thấy", $"Không có quán phù hợp với: {keyword}", "Đóng");
            return;
        }

        ApplySearchResults(matchedPois, focusOnFirst: true);
    }

    private async void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
        _searchDebounceCts = new CancellationTokenSource();
        var debounceToken = _searchDebounceCts.Token;

        SearchClearButton.IsVisible = !string.IsNullOrWhiteSpace(e.NewTextValue);

        var keyword = e.NewTextValue?.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            _searchSuggestions = new List<POI>();
            SearchSuggestionsView.ItemsSource = null;
            SearchSuggestionsContainer.IsVisible = false;
            ClearSearchState();
            return;
        }

        try
        {
            await Task.Delay(220, debounceToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (debounceToken.IsCancellationRequested)
        {
            return;
        }

        if (_pois.Count == 0)
        {
            _pois = await _poiService.GetAllPOIsAsync();
        }

        var searchablePois = GetInteractivePois();

        _searchSuggestions = FindMatchingPois(keyword, maxResults: 6, sourcePois: searchablePois);
        SearchSuggestionsView.ItemsSource = _searchSuggestions;
        SearchSuggestionsContainer.IsVisible = _searchSuggestions.Count > 0;

        var highlightedMatches = FindMatchingPois(keyword, maxResults: searchablePois.Count, sourcePois: searchablePois);
        if (highlightedMatches.Count > 0)
        {
            _searchHighlightedPoiIds = highlightedMatches
                .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId))
                .Select(p => p.restaurantId)
                .ToHashSet(StringComparer.Ordinal);
            ApplyPoiModeFromState(focusOnTour: false);
        }
        else
        {
            ClearSearchState();
        }
    }

    private void OnSearchSuggestionSelected(object? sender, SelectionChangedEventArgs e)
    {
        var selectedPoi = e.CurrentSelection.FirstOrDefault() as POI;
        if (selectedPoi == null)
        {
            return;
        }

        SearchSuggestionsView.SelectedItem = null;
        SearchEntry.Text = selectedPoi.Name;
        SearchSuggestionsContainer.IsVisible = false;
        ApplySearchResults(new List<POI> { selectedPoi }, focusOnFirst: true);
    }

    private void OnClearSearchTapped(object? sender, TappedEventArgs e)
    {
        SearchEntry.Text = string.Empty;
        SearchSuggestionsContainer.IsVisible = false;
        SearchSuggestionsView.ItemsSource = null;
        ClearSearchState();
    }

    // áp dụng kết quả tìm kiếm
    private void ApplySearchResults(List<POI> pois, bool focusOnFirst)
    {
        _searchHighlightedPoiIds = pois
            .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId))
            .Select(p => p.restaurantId)
            .ToHashSet(StringComparer.Ordinal);

        var firstPoi = pois.FirstOrDefault();
        if (firstPoi != null)
        {
            ShowSelectedPoiCard(firstPoi);
            if (focusOnFirst)
            {
                CenterMapOn(firstPoi.Latitude, firstPoi.Longitude, MyLocationZoomLevel);
            }
        }
        else
        {
            HideSelectedPoiCard();
        }

        ApplyPoiModeFromState(focusOnTour: false);
    }

    // dọn ô liên quan đến tìm kiếm
    private void ClearSearchState()
    {
        _searchHighlightedPoiIds.Clear();
        HideSelectedPoiCard();
        ApplyPoiModeFromState(focusOnTour: false);
    }


    // tìm những poi match với từ khóa tim kiếm
    private List<POI> FindMatchingPois(string keyword, int maxResults, IEnumerable<POI>? sourcePois = null)
    {
        var normalizedKeyword = NormalizeSearchText(keyword);
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return new List<POI>();
        }

        var candidates = sourcePois?.ToList() ?? _pois;
        var safeMaxResults = Math.Max(1, Math.Min(maxResults, candidates.Count == 0 ? 1 : candidates.Count));

        var matches = candidates
            .Select(poi => new
            {
                Poi = poi,
                Score = GetSearchScore(poi, normalizedKeyword)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Poi.Name?.Length ?? int.MaxValue)
            .Take(safeMaxResults)
            .Select(x => x.Poi)
            .ToList();

        return matches;
    }

    // Chấm điểm (ranking) độ liên quan của một POI so với từ khóa tìm kiếm
    private static int GetSearchScore(POI poi, string normalizedKeyword)
    {
        var name = NormalizeSearchText(poi.Name ?? string.Empty);
        var address = NormalizeSearchText(poi.AddressDisplay ?? string.Empty);
        var id = NormalizeSearchText(poi.restaurantId ?? string.Empty);

        if (name.Equals(normalizedKeyword, StringComparison.Ordinal)) return 300;
        if (name.StartsWith(normalizedKeyword, StringComparison.Ordinal)) return 200;
        if (name.Contains(normalizedKeyword, StringComparison.Ordinal)) return 150;
        if (address.Contains(normalizedKeyword, StringComparison.Ordinal)) return 100;
        if (id.Contains(normalizedKeyword, StringComparison.Ordinal)) return 80;

        return 0;
    }

    // chuẩn hóa chuỗi tìm kiếm
    private static string NormalizeSearchText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input.Trim().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);

        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(char.ToLowerInvariant(ch));
            }
        }

        return builder
            .ToString()
            .Normalize(NormalizationForm.FormC);
    }

    private void OnMapFilterAllTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.All);
    }

    private void OnMapFilterNearTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.Nearby);
    }

    private void OnMapFilterFavoriteTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.Favorite);
    }

    private void OnMapFilterOpenTapped(object sender, TappedEventArgs e)
    {
        SetPoiFilter(PoiCategoryFilter.OpenNow);
    }

    // Đổi bộ lọc POI (category) và reset lại toàn bộ trạng thái UI + dữ liệu liên quan
    private void SetPoiFilter(PoiCategoryFilter filter)
    {
        _activePoiFilter = filter;
        _searchHighlightedPoiIds.Clear();
        SearchSuggestionsContainer.IsVisible = false;
        SearchSuggestionsView.ItemsSource = null;
        HideSelectedPoiCard();
        UpdateCategoryFilterUi();
        ApplyPoiModeFromState(focusOnTour: false);
    }

    // cập nhật trạng thái hienr thị của các filter trên UI
    private void UpdateCategoryFilterUi()
    {
        SetMapChipState(MapFilterAllChip, MapFilterAllLabel, _activePoiFilter == PoiCategoryFilter.All, MapFilterAllIcon);
        SetMapChipState(MapFilterNearChip, MapFilterNearLabel, _activePoiFilter == PoiCategoryFilter.Nearby, MapFilterNearIcon);
        SetMapChipState(MapFilterFavoriteChip, MapFilterFavoriteLabel, _activePoiFilter == PoiCategoryFilter.Favorite, MapFilterFavoriteIcon);
        SetMapChipState(MapFilterOpenChip, MapFilterOpenLabel, _activePoiFilter == PoiCategoryFilter.OpenNow, MapFilterOpenIcon);
    }

    // cập nhật trạng thái hiển thị của filter trên bản đồ
    private static void SetMapChipState(Border border, Label textLabel, bool isActive, Label? iconLabel = null)
    {
        border.BackgroundColor = isActive ? Color.FromArgb("#F48C06") : Colors.White;
        textLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#3C4043");
        if (iconLabel != null)
        {
            iconLabel.TextColor = isActive ? Colors.White : Color.FromArgb("#3C4043");
        }
    }

    // cập nhật dánh sách POI hiển thị dựa vào filter đang áp dụng
    private List<POI> ApplyCategoryFilter(IEnumerable<POI> source)
    {
        var candidates = source.ToList();
        if (candidates.Count == 0)
        {
            return candidates;
        }

        switch (_activePoiFilter)
        {
            case PoiCategoryFilter.Nearby:
                var location = _lastKnownLocation ?? _locationService.LastKnownLocation;
                if (location == null)
                {
                    return new List<POI>();
                }

                return candidates
                    .Where(p => _poiService.GetDistanceMeters(location, p) <= NearbyFilterRadiusMeters)
                    .ToList();

            case PoiCategoryFilter.Favorite:
                var favoriteIds = _favoriteService
                    .GetFavorites()
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                return candidates
                    .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId) && favoriteIds.Contains(p.restaurantId))
                    .ToList();

            case PoiCategoryFilter.OpenNow:
                return candidates.Where(p => p.IsCurrentlyOpen).ToList();

            case PoiCategoryFilter.All:
            default:
                return candidates;
        }
    }


    // áp dụng state hiện tại (bao gồm filter hành trình, filter category, và search) để cập nhật lại trạng thái hiển thị của các POI trên bản đồ, bao gồm việc highlight POI nào, ẩn hiện card thông tin POI nào, v.v. Hàm này sẽ được gọi mỗi khi có sự thay đổi về state filter hoặc vị trí người dùng để đảm bảo rằng bản đồ luôn phản ánh đúng state hiện tại
    private void ApplyPoiModeFromState(bool focusOnTour)
    {
        if (!_isMapLoaded || mapControl?.Map == null)
        {
            return;
        }

        UpdateTourFilterBanner();
        var visiblePoiIds = GetVisiblePoiIds();

        if (_searchHighlightedPoiIds.Count > 0)
        {
            MapHelper.HighlightPOIs(
                mapControl,
                _searchHighlightedPoiIds,
                isSearchResult: true,
                visiblePoiIds: visiblePoiIds,
                tourStopOrdersByPoiId: GetTourStopOrdersForCurrentMode());
            return;
        }

        if (_tourPoiFilterIds.Count > 0)
        {
            var tourPois = GetInteractivePois();
            var tourLocation = _lastKnownLocation ?? _locationService.LastKnownLocation;
            var nearestTourPoi = tourLocation == null
                ? null
                : _poiService.GetNearestPOI(tourLocation, tourPois);

            var shouldHighlightNearestTourPoi = tourLocation != null
                && nearestTourPoi != null
                && _poiService.GetDistanceMeters(tourLocation, nearestTourPoi) < AppSettings.MapHighlightDistanceMeters;

            if (focusOnTour && tourPois.Count > 0)
            {
                var focusPoi = nearestTourPoi ?? tourPois[0];
                CenterMapOn(focusPoi.Latitude, focusPoi.Longitude, MyLocationZoomLevel);
                ShowSelectedPoiCard(focusPoi);
            }

            MapHelper.HighlightPOIs(
                mapControl,
                shouldHighlightNearestTourPoi ? new[] { nearestTourPoi!.restaurantId } : null,
                visiblePoiIds: visiblePoiIds,
                tourStopOrdersByPoiId: GetTourStopOrdersForCurrentMode());
            return;
        }

        if (_selectedPoi != null)
        {
            MapHelper.HighlightPOI(mapControl, _selectedPoi, visiblePoiIds: visiblePoiIds);
            return;
        }

        var location = _lastKnownLocation ?? _locationService.LastKnownLocation;
        var interactivePois = GetInteractivePois();
        var nearest = location == null
            ? null
            : _poiService.GetNearestPOI(location, interactivePois);

        var shouldHighlightNearest = location != null
            && nearest != null
            && _poiService.GetDistanceMeters(location, nearest) < AppSettings.MapHighlightDistanceMeters;

        MapHelper.HighlightPOI(
            mapControl,
            shouldHighlightNearest ? nearest : null,
            visiblePoiIds: visiblePoiIds);
    }

    // Lấy danh sách POI “có thể tương tác” (click / hiển thị / xử lý UI) dựa trên trạng thái hiện tại (tour + category filter) 
    private List<POI> GetInteractivePois()
    {
        var scopedPois = _tourPoiFilterIds.Count == 0
            ? _pois
            : _pois
                .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId) && _tourPoiFilterIds.Contains(p.restaurantId))
                .ToList();

        return ApplyCategoryFilter(scopedPois);
    }
    
    // Lấy danh sách các POI ID đang hiển thị trên bản đồ dựa trên state filter hiện tại (bao gồm filter hành trình và filter category), để chỉ highlight các POI đó trên bản đồ, giúp giảm thiểu tình trạng bị highlight nhầm các POI không nằm trong filter hiện tại
    private IEnumerable<string>? GetVisiblePoiIds()
    {
        var hasTourFilter = _tourPoiFilterIds.Count > 0;
        var hasCategoryFilter = _activePoiFilter != PoiCategoryFilter.All;

        if (!hasTourFilter && !hasCategoryFilter)
        {
            return null;
        }

        return GetInteractivePois()
            .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId))
            .Select(p => p.restaurantId)
            .ToList();
    }

    // Cập nhật banner hiển thị thông tin về filter hành trình đang áp dụng, nếu có. Nếu không có filter hành trình thì ẩn banner này đi để tiết kiệm không gian hiển thị bản đồ
    private void UpdateTourFilterBanner()
    {
        if (TourFilterBanner == null || TourFilterTitle == null)
        {
            return;
        }

        var isTourMode = _tourPoiFilterIds.Count > 0;
        TourFilterBanner.IsVisible = isTourMode;
        if (CategoryFiltersScrollView != null)
        {
            CategoryFiltersScrollView.IsVisible = !isTourMode;
        }

        if (!isTourMode)
        {
            return;
        }

        TourFilterTitle.Text = string.IsNullOrWhiteSpace(_activeTourName)
            ? "Đang xem theo hành trình"
            : $"Hành trình: {_activeTourName}";
    }

    // Chuyển một chuỗi chứa nhiều POI ID (dạng text) thành HashSet<string> sạch, không trùng, dễ xử lý hơn, và đảm bảo rằng nếu có lỗi xảy ra trong quá trình parse thì sẽ trả về một HashSet rỗng thay vì làm hỏng chức năng của trang
    private static HashSet<string> ParsePoiIdSet(string? rawPoiIds)
    {
        var decoded = DecodeQueryValue(rawPoiIds);
        if (string.IsNullOrWhiteSpace(decoded))
        {
            return new HashSet<string>(StringComparer.Ordinal);
        }

        return decoded
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .ToHashSet(StringComparer.Ordinal);
    }

    // Hàm này để parse map từ restaurantId sang stop order của các POI trong tour, giúp cho việc hiển thị số thứ tự dừng trên marker của POI khi đang ở chế độ tour
    private static Dictionary<string, int> ParsePoiStopOrderMap(string? rawStopOrders)
    {
        var decoded = DecodeQueryValue(rawStopOrders);
        if (string.IsNullOrWhiteSpace(decoded))
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        var result = new Dictionary<string, int>(StringComparer.Ordinal);
        var pairs = decoded.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split(':', 2, StringSplitOptions.TrimEntries);
            if (parts.Length != 2)
            {
                continue;
            }

            var restaurantId = parts[0];
            if (string.IsNullOrWhiteSpace(restaurantId))
            {
                continue;
            }

            if (!int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var stopOrder)
                || stopOrder <= 0
                || result.ContainsKey(restaurantId))
            {
                continue;
            }

            result[restaurantId] = stopOrder;
        }

        return result;
    }

    // Lấy thứ tự các điểm dừng (tour stop order) của POI khi đang ở chế độ tour
    private IReadOnlyDictionary<string, int>? GetTourStopOrdersForCurrentMode()
    {
        if (_tourPoiFilterIds.Count == 0 || _tourStopOrdersByPoiId.Count == 0)
        {
            return null;
        }

        return _tourStopOrdersByPoiId;
    }
    
    // Hàm này để giải mã giá trị query parameter đã được mã hóa trước đó, đảm bảo rằng nếu có lỗi xảy ra trong quá trình giải mã thì sẽ trả về giá trị gốc thay vì làm hỏng chức năng của trang
    private static string? DecodeQueryValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        try
        {
            return Uri.UnescapeDataString(value);
        }
        catch
        {
            return value;
        }
    }

    // Khi người dùng nhấn vào nút xóa filter hành trình, sẽ xóa bỏ filter hành trình hiện tại, clear scope của narration flow để có thể tự động đọc tất cả POI như bình thường, và cập nhật lại bản đồ để hiển thị tất cả POI
    private void OnClearTourFilterClicked(object sender, EventArgs e)
    {
        _tourPoiIdsRaw = string.Empty;
        _tourStopOrdersRaw = string.Empty;
        _activeTourName = null;
        _tourPoiFilterIds.Clear();
        _tourStopOrdersByPoiId.Clear();
        _narrationFlowService.ClearAutoNarrationPoiScope();
        _searchHighlightedPoiIds.Clear();
        HideSelectedPoiCard();
        ApplyPoiModeFromState(focusOnTour: false);
    }

    // Khi có filter hành trình, sẽ set scope cho narration flow để chỉ tự động đọc các POI trong hành trình đó, nếu có. Nếu không có filter hành trình thì clear scope để có thể tự động đọc tất cả POI như bình thường
    private void ApplyNarrationScopeFromTourFilter()
    {
        if (_tourPoiFilterIds.Count > 0)
        {
            _narrationFlowService.SetAutoNarrationPoiScope(_tourPoiFilterIds);
            return;
        }

        _narrationFlowService.ClearAutoNarrationPoiScope();
    }

    // Hiển thị card thông tin của POI đã chọn, bao gồm tên, địa chỉ, hình ảnh và các thông tin liên quan khác. Card này sẽ được hiển thị ở dưới cùng
    private void ShowSelectedPoiCard(POI poi)
    {
        _selectedPoi = poi;
        SelectedPoiName.Text = string.IsNullOrWhiteSpace(poi.Name) ? poi.restaurantId : poi.Name;
        SelectedPoiAddress.Text = poi.AddressDisplay;
        SelectedPoiImage.Source = poi.PrimaryImage;
        SelectedPoiCard.IsVisible = true;
    }

    // Ẩn card POI đã chọn và xóa bỏ POI được chọn hiện tại
    private void HideSelectedPoiCard()
    {
        _selectedPoi = null;
        SelectedPoiCard.IsVisible = false;
    }

    // Hàm này để hiển thị thông báo cho người dùng khi họ không có kết nối Internet và cũng không có dữ liệu bản đồ offline nào đã được lưu trước đó, nhằm giải thích rằng họ vẫn có thể xem và tương tác với các địa điểm trên bản đồ nhưng nền bản đồ có thể không hiển thị được, và đảm bảo rằng thông báo này chỉ được hiển thị một lần
    private async Task ShowOfflineMapNoticeIfNeededAsync()
    {
        if (_hasShownOfflineMapUnavailableNotice)
        {
            return;
        }

        if (Connectivity.NetworkAccess == NetworkAccess.Internet)
        {
            return;
        }

        if (MapHelper.HasCachedMapTiles())
        {
            return;
        }

        _hasShownOfflineMapUnavailableNotice = true;
        await DisplayAlertAsync(
            "Bản đồ offline",
            "Hiện không có Internet và chưa có dữ liệu nền bản đồ đã lưu. Bạn vẫn xem và tương tác được các địa điểm, nhưng nền bản đồ có thể chưa hiển thị.",
            "Đóng");
    }

    // nhấn vào nút xem chi tiết trên card POI đã chọn sẽ điều hướng đến trang chi tiết của POI đó, nếu có restaurantId hợp lệ
    private async void OnViewDetailClicked(object sender, EventArgs e)
    {
        if (_selectedPoi == null || string.IsNullOrWhiteSpace(_selectedPoi.restaurantId))
        {
            return;
        }

        var encodedId = Uri.EscapeDataString(_selectedPoi.restaurantId);
        await Shell.Current.GoToAsync($"{nameof(POIDetailPage)}?restaurantId={encodedId}");
    }
}