using food_market_narrator.Models;
using food_market_narrator.Services;
using Microsoft.Maui.Controls.Maps;
using Microsoft.Maui.Maps;


namespace food_market_narrator.Helpers
{
    public static class MapHelper
    {
        // Dùng để load map với các POI, có thể gọi khi trang map được hiển thị và move to region nếu có location ban đầu
        public static async Task LoadMap(Microsoft.Maui.Controls.Maps.Map map, POIService poiService, Location? initialLocation = null)
        {
            try
            {
                // 1. Determine location to focus
                Location focusLocation = initialLocation;

                if (focusLocation == null)
                {
                     // Check permissions
                     var status = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
                     if (status != PermissionStatus.Granted)
                     {
                         status = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                     }

                     if (status == PermissionStatus.Granted)
                     {
                         var request = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));
                         focusLocation = await Geolocation.Default.GetLocationAsync(request);
                     }
                }

                if (focusLocation != null)
                {
                    // Move map
                    map.MoveToRegion(MapSpan.FromCenterAndRadius(focusLocation, Distance.FromKilometers(0.5)));
                }

                // 2. Load POIs (Pins)
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
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading map: {ex.Message}");
            }
        }
    }
}
