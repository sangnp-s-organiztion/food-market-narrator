using food_market_narrator.Helpers;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using Mapsui;
using Mapsui.Projections;
using Mapsui.UI.Maui;

namespace food_market_narrator.Views;

[QueryProperty(nameof(Latitude), "lat")]
[QueryProperty(nameof(Longitude), "lng")]
[QueryProperty(nameof(LocationName), "name")]

public partial class MapPage : ContentPage
{
    private const int DefaultZoomLevel = 16;
    private const int MinZoomLevel = 3;
    private const int MaxZoomLevel = 20;
    private const int MyLocationZoomLevel = 18;

    private readonly IPOIService _poiService;
    private readonly ILocationService _locationService;

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

        // Tải dữ liệu bản đồ và hiển thị các POI, cũng như vị trí người dùng nếu đã có
        await MapHelper.LoadMapAsync(
        mapControl,
        _poiService,
        _locationService);
    }

    protected override void OnDisappearing()
    {
        _locationService.LocationChanged -= OnLocationChangedForMap;
        base.OnDisappearing();
    }

    // cập nhật vị trí trên bản đồ khi vị trí ng dùng changed và kiểm tra xem có POI nào gần đó để 2light không
    private void OnLocationChangedForMap(object? sender, Location location)
    {
        // Cập nhật vị trí người dùng trên bản đồ và kiểm tra POI gần nhất
        MainThread.BeginInvokeOnMainThread(() =>
        {
            MapHelper.UpdateUserLocation(mapControl, location.Latitude, location.Longitude);
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
            await DisplayAlert("Thông báo", "Không thể lấy vị trí hiện tại.", "OK");
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
        if (viewport == null)
        {
            return;
        }

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
}