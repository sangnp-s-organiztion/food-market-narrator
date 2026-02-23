using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using food_market_narrator.Models;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;

namespace food_market_narrator;

public class CustomMapHandler : MapHandler
{
    public static GoogleMap? NativeGoogleMap;
    public static event Action<GoogleMap>? OnGoogleMapReady;

    public static Dictionary<string, Marker> MarkerDictionary = new();

    public static void SetPOIMarkers(IEnumerable<POI> pois)
    {
        if (NativeGoogleMap == null)
            return;

        NativeGoogleMap.Clear();
        MarkerDictionary.Clear();

        foreach (var poi in pois)
        {
            var markerOptions = new MarkerOptions()
                .SetPosition(new LatLng(poi.Latitude, poi.Longitude))
                .SetTitle(poi.Name ?? poi.restaurantId)
                .SetSnippet(poi.Address)
                .SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange));

            var marker = NativeGoogleMap.AddMarker(markerOptions);
            if (marker != null)
            {
                MarkerDictionary[poi.restaurantId] = marker;
            }
        }
    }

    public static void HighlightMarker(string? poiId)
    {
        if (NativeGoogleMap == null)
            return;

        foreach (var marker in MarkerDictionary.Values)
        {
            marker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueOrange));
        }

        if (!string.IsNullOrWhiteSpace(poiId) && MarkerDictionary.TryGetValue(poiId, out var nearestMarker))
        {
            nearestMarker.SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueRed));
            nearestMarker.ShowInfoWindow();
        }
    }

    protected override void ConnectHandler(MapView platformView)
    {
        base.ConnectHandler(platformView);
        platformView.GetMapAsync(new MapReadyCallback());
    }

    class MapReadyCallback : Java.Lang.Object, IOnMapReadyCallback
    {
        public void OnMapReady(GoogleMap googleMap)
        {
            NativeGoogleMap = googleMap;
            OnGoogleMapReady?.Invoke(googleMap);
        }
    }
}