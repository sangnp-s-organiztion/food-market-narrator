using food_market_narrator.Helpers;
using food_market_narrator.Settings;
using food_market_narrator.Services;
using Mapsui.UI.Maui;

namespace food_market_narrator.Views;

[QueryProperty(nameof(Latitude), "lat")]
[QueryProperty(nameof(Longitude), "lng")]
[QueryProperty(nameof(LocationName), "name")]

public partial class MapPage : ContentPage
{
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
}