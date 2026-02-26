using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;
#if ANDROID
using Android.Gms.Maps;
#endif


namespace food_market_narrator.Helpers
{
    public static class MapHelper
    {
        // Dùng để load map với các POI, có thể gọi khi trang map được hiển thị và move to region nếu có location ban đầu
        public static async Task LoadMapAsync(
            Microsoft.Maui.Controls.Maps.Map map,
            POIService poiService,
            ILocationService locationService,
            Location? initialLocation = null)
        {
            try
            {
                Location? focusLocation = initialLocation;

                if (focusLocation == null)
                {
                    focusLocation = await locationService.GetCurrentLocationAsync();
                }

                if (focusLocation != null)
                {
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(focusLocation, Distance.FromKilometers(0.5)));
                }

                var pois = await poiService.GetPOIsAsync();
                
                map.Pins.Clear();
                foreach (var poi in pois)
                {
                   var pin = new Pin
                   {
                       Label = poi.Name,
                       Address = poi.Address,
                       Type = PinType.Place,
                       Location = new Location(poi.Latitude, poi.Longitude)
                   };
                   map.Pins.Add(pin);
                }

#if ANDROID
                void RegisterNativeMarkers()
                {
                    CustomMapHandler.SetPOIMarkers(pois);
                }

                if (CustomMapHandler.NativeGoogleMap != null)
                {
                    RegisterNativeMarkers();
                }
                else
                {
                    void OnMapReady(GoogleMap _)
                    {
                        RegisterNativeMarkers();
                        CustomMapHandler.OnGoogleMapReady -= OnMapReady;
                    }

                    CustomMapHandler.OnGoogleMapReady += OnMapReady;
                }
#endif

                if (focusLocation != null)
                {
                    var nearest = poiService.GetNearestPOI(focusLocation.Latitude, focusLocation.Longitude);
                    poiService.HighlightNearestPOI(map, nearest);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading map: {ex.Message}");
            }
        }
    }
}
