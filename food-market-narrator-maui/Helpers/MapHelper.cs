using food_market_narrator.Controls;
using food_market_narrator.Services;

namespace food_market_narrator.Helpers
{
    public static class MapHelper
    {
        /// <summary>
        /// Load (or refresh) the MapLibre map:
        ///  1. First call  → starts the tile-server if local tiles exist, loads map.html,
        ///                    waits for MapLibre to initialise, then pushes markers.
        ///  2. Subsequent  → just refreshes markers and pans to current location
        ///                    (map HTML stays alive in the WebView).
        /// </summary>
        public static async Task LoadMapAsync(
            MapWebView mapWebView,
            POIService poiService,
            ILocationService locationService,
            TileServerService tileServerService,
            Location? initialLocation = null)
        {
            try
            {
                Location? focusLocation = initialLocation
                    ?? await locationService.GetCurrentLocationAsync();

                if (!mapWebView.IsMapReady)
                {
                    // Start local tile-server if a PMTiles file is present
                    if (tileServerService.HasLocalTiles)
                        tileServerService.Start();

                    mapWebView.LoadMap(
                        useLocalTiles: tileServerService.HasLocalTiles,
                        tileServerPort: TileServerService.Port);

                    // Wait for MapLibre 'load' event (JS fires maui://mapReady)
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                    await mapWebView.WhenMapReadyAsync(cts.Token);
                }

                var pois = await poiService.GetPOIsAsync();
                await mapWebView.AddMarkersAsync(pois);

                if (focusLocation != null)
                {
                    await mapWebView.UpdateUserLocationAsync(
                        focusLocation.Latitude, focusLocation.Longitude);

                    await mapWebView.MoveToLocationAsync(
                        focusLocation.Latitude, focusLocation.Longitude, zoom: 15);

                    var nearest = poiService.GetNearestPOI(
                        focusLocation.Latitude, focusLocation.Longitude);
                    poiService.HighlightNearestPOI(mapWebView, nearest);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[MapHelper] Timed out waiting for map to initialise.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MapHelper] Error loading map: {ex.Message}");
            }
        }
    }
}
