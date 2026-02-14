using food_market_narrator.Models;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
using MauiMap = Microsoft.Maui.Controls.Maps.Map;
using Android.Gms.Maps.Model;
using System.Net.Http.Json;



namespace food_market_narrator.Services;

public class POIService
{
    private POI? _lastNearest;
    private bool _isInsidePOI = false;
    private List<POI> _pois;
    private const double EnterRadius = 30; // mét
    private const double ExitRadius = 40;  // mét

    // Danh sach cac POI
    private readonly HttpClient _httpClient;

    public POIService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<List<POI>> GetPOIsAsync()
    {
        var url = "http://10.0.2.2:5044/api/restaurant";
        // Nếu chạy Windows local app thì dùng localhost

        var data = await _httpClient.GetFromJsonAsync<List<POI>>(url);

        if (data == null)
            return new List<POI>();

        // xử lý audio file giống như m làm trước đó
        foreach (var poi in data)
        {
            var originalId = poi.restaurantId;
            var lastDashIndex = originalId.LastIndexOf('-');

            var audioFileName = lastDashIndex > 0
                ? originalId.Substring(0, lastDashIndex) + ".mp3"
                : originalId + ".mp3";

            poi.AudioFile = audioFileName;
        }

        return data;
    }

    public void SetPOIs(List<POI> pois)
    {
        _pois = pois;
    }

    // Lấy tất cả các POIs
    public List<POI> GetAllPOIs()
    {
        return _pois;
    }

    // Lấy tất cả các POIs đồng bộ
    public async Task<List<POI>> GetAllPOIsAsync()
    {
        return await Task.FromResult(_pois);
    }

    // Lấy POI gần nhất dựa trên vị trí hiện tại và các POIs
    public POI? UpdateNearestPOI(double currentLat, double currentLng)
    {
        if (_pois == null || !_pois.Any())
            return null;

        var currentLocation = new Location(currentLat, currentLng);

        POI? nearest = null;
        double minDistance = double.MaxValue;

        // Tìm POI gần nhất
        foreach (var poi in _pois)
        {
            var poiLocation = new Location(poi.Latitude, poi.Longitude);

            var distance = Location.CalculateDistance(
                currentLocation,
                poiLocation,
                DistanceUnits.Kilometers) * 1000; // đổi sang mét

            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = poi;
            }
        }

        if (nearest == null)
            return null;

        if (!_isInsidePOI)
        {
            // Chưa ở trong POI → xét EnterRadius
            if (minDistance <= EnterRadius)
            {
                _isInsidePOI = true;
                _lastNearest = nearest;

                return nearest; // Trigger khi mới vào
            }
        }
        else
        {
            // Đang ở trong POI
            // Nếu đổi sang POI khác và đủ gần
            if (nearest != _lastNearest && minDistance <= EnterRadius)
            {
                _lastNearest = nearest;
                return nearest; // Trigger POI mới
            }

            // Nếu đi xa khỏi POI hiện tại > ExitRadius
            if (_lastNearest != null)
            {
                var lastLocation = new Location(
                    _lastNearest.Latitude,
                    _lastNearest.Longitude);

                var distanceFromLast = Location.CalculateDistance(
                    currentLocation,
                    lastLocation,
                    DistanceUnits.Kilometers) * 1000;

                if (distanceFromLast > ExitRadius)
                {
                    _isInsidePOI = false;
                    _lastNearest = null;
                }
            }
        }

        return null; // Không có thay đổi
    }

    // Hightlight POI gần nhất
    public void HighlightNearestPOI(MauiMap map, POI? nearest)
    {
        if (_pois == null || !_pois.Any())
            return;

        #if ANDROID
        foreach (var pair in CustomMapHandler.MarkerDictionary)
        {
            var marker = pair.Value;

            if (nearest != null && pair.Key == nearest.restaurantId)
            {
                marker.SetIcon(
                    BitmapDescriptorFactory.DefaultMarker(
                        BitmapDescriptorFactory.HueBlue));
            }
            else
            {
                marker.SetIcon(
                    BitmapDescriptorFactory.DefaultMarker(
                        BitmapDescriptorFactory.HueRed));
            }
        }
        #endif

        // tự động zoom map về POI gần nhất trong bán kính
        if (nearest != null)
        {
            map.MoveToRegion(
                MapSpan.FromCenterAndRadius(
                    new Location(nearest.Latitude, nearest.Longitude),
                    Distance.FromMeters(10)));
        }
    }





}