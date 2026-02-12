using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Microsoft.Maui.Maps;
using Microsoft.Maui.Maps.Handlers;

namespace food_market_narrator;

public class CustomMapHandler : MapHandler
{
    public static GoogleMap? NativeGoogleMap;
    public static Dictionary<string, Marker> MarkerDictionary = new();

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
        }
    }
}