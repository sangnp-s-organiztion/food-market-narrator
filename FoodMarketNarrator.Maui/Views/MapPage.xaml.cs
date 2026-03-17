using food_market_narrator.Helpers;
using food_market_narrator.Models;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;
using System.Globalization;
using System.Text;

namespace food_market_narrator.Views;

[QueryProperty(nameof(Latitude), "lat")]
[QueryProperty(nameof(Longitude), "lng")]
[QueryProperty(nameof(LocationName), "name")]

public partial class MapPage : ContentPage
{
    private const double MarkerTapPixelRadius = 28;
    private const int DefaultZoomLevel = 16;
    private const int MinZoomLevel = 3;
    private const int MaxZoomLevel = 20;
    private const int MyLocationZoomLevel = 18;

    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;
    private List<POI> _pois = new();
    private List<POI> _searchSuggestions = new();
    private POI? _selectedPoi;
    private HashSet<string> _searchHighlightedPoiIds = new(StringComparer.Ordinal);
    private bool _isMapLoaded;
    private CancellationTokenSource? _searchDebounceCts;

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? LocationName { get; set; }

    public MapPage(
        IPOIService poiService,
        ILocationService locationService)
    {
        InitializeComponent();
        _poiService = poiService;
        _locationService = locationService;
    }

    // Khi trang xuất hiện, bắt đầu theo dõi vị trí và tải dữ liệu bản đồ
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        // Hủy đăng ký hàm xử lí sự kiện thay đổi vị trí ng dùng để tránh bị đăng ký nhiều lần nếu người dùng vào ra trang nhiều lần
        _locationService.LocationChanged -= OnLocationChangedForMap;

        // Đăng ký lại hàm xử lí sự kiện thay đổi vị trí ng dùng để cập nhật bản đồ khi vị trí thay đổi
        _locationService.LocationChanged += OnLocationChangedForMap;
        await _locationService.StartTrackingAsync();

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

        if (_pois.Count == 0)
        {
            _pois = await _poiService.GetAllPOIsAsync();
        }

        mapControl.MapTapped -= OnMapTapped;
        mapControl.MapTapped += OnMapTapped;

        SearchClearButton.IsVisible = !string.IsNullOrWhiteSpace(SearchEntry.Text);
        SearchSuggestionsContainer.IsVisible = false;

        HideSelectedPoiCard();
    }

    protected override void OnDisappearing()
    {
        mapControl.MapTapped -= OnMapTapped;

        _locationService.LocationChanged -= OnLocationChangedForMap;
        _searchDebounceCts?.Cancel();
        base.OnDisappearing();
    }

    // cập nhật vị trí trên bản đồ khi vị trí ng dùng changed và kiểm tra xem có POI nào gần đó để 2light không
    private void OnLocationChangedForMap(object? sender, Location location)
    {
        // Cập nhật vị trí người dùng trên bản đồ và kiểm tra POI gần nhất
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MapHelper.UpdateUserLocation(mapControl, location.Latitude, location.Longitude);

            if (_searchHighlightedPoiIds.Count > 0)
            {
                MapHelper.HighlightPOIs(mapControl, _searchHighlightedPoiIds, isSearchResult: true);
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

        CenterMapOn(currentLocation.Latitude, currentLocation.Longitude, MyLocationZoomLevel);
        MapHelper.UpdateUserLocation(mapControl, currentLocation.Latitude, currentLocation.Longitude);
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

        if (_pois.Count == 0)
        {
            HideSelectedPoiCard();
            return;
        }

        if (e.WorldPosition == null)
        {
            HideSelectedPoiCard();
            return;
        }

        var tapLonLat = SphericalMercator.ToLonLat(e.WorldPosition.X, e.WorldPosition.Y);
        var tappedLocation = new Location(tapLonLat.lat, tapLonLat.lon);
        var nearestPoi = _poiService.GetNearestPOI(tappedLocation, _pois);

        if (nearestPoi == null)
        {
            HideSelectedPoiCard();
            return;
        }

        var viewportResolution = mapControl.Map?.Navigator?.Viewport.Resolution ?? ToResolution(DefaultZoomLevel);
        var tapThresholdMeters = Math.Clamp(viewportResolution * MarkerTapPixelRadius, 12, 150);
        var distanceMeters = _poiService.GetDistanceMeters(tappedLocation, nearestPoi);

        if (distanceMeters > tapThresholdMeters)
        {
            HideSelectedPoiCard();
            return;
        }

        ShowSelectedPoiCard(nearestPoi);
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

        var matchedPois = FindMatchingPois(keyword, maxResults: _pois.Count);
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

        _searchSuggestions = FindMatchingPois(keyword, maxResults: 6);
        SearchSuggestionsView.ItemsSource = _searchSuggestions;
        SearchSuggestionsContainer.IsVisible = _searchSuggestions.Count > 0;

        var highlightedMatches = FindMatchingPois(keyword, maxResults: _pois.Count);
        if (highlightedMatches.Count > 0)
        {
            _searchHighlightedPoiIds = highlightedMatches
                .Where(p => !string.IsNullOrWhiteSpace(p.restaurantId))
                .Select(p => p.restaurantId)
                .ToHashSet(StringComparer.Ordinal);
            MapHelper.HighlightPOIs(mapControl, _searchHighlightedPoiIds, isSearchResult: true);
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

        MapHelper.HighlightPOIs(mapControl, _searchHighlightedPoiIds, isSearchResult: true);
    }

    private void ClearSearchState()
    {
        _searchHighlightedPoiIds.Clear();
        HideSelectedPoiCard();
        MapHelper.HighlightPOIs(mapControl, null);
    }

    private List<POI> FindMatchingPois(string keyword, int maxResults)
    {
        var normalizedKeyword = NormalizeSearchText(keyword);
        if (string.IsNullOrWhiteSpace(normalizedKeyword))
        {
            return new List<POI>();
        }

        var matches = _pois
            .Select(poi => new
            {
                Poi = poi,
                Score = GetSearchScore(poi, normalizedKeyword)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Poi.Name?.Length ?? int.MaxValue)
            .Take(Math.Max(1, maxResults))
            .Select(x => x.Poi)
            .ToList();

        return matches;
    }

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

    private void ShowSelectedPoiCard(POI poi)
    {
        _selectedPoi = poi;
        SelectedPoiName.Text = string.IsNullOrWhiteSpace(poi.Name) ? poi.restaurantId : poi.Name;
        SelectedPoiAddress.Text = poi.AddressDisplay;
        SelectedPoiImage.Source = poi.PrimaryImage;
        SelectedPoiCard.IsVisible = true;
    }

    private void HideSelectedPoiCard()
    {
        _selectedPoi = null;
        SelectedPoiCard.IsVisible = false;
    }

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