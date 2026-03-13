using BruTile.Predefined;
using BruTile.Web;
using food_market_narrator.Models;
using food_market_narrator.Services;
using Mapsui;
using Mapsui.Extensions;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Styles;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Maui;

namespace food_market_narrator.Helpers
{
    public static class MapHelper
    {
        private const string PoiLayerName = "POIs";
        private const string UserLocationLayerName = "UserLocation";
        private const string OsmLayerName = "OpenStreetMap";

        public static async Task LoadMapAsync(
            MapControl mapControl,
            IPOIService poiService,
            ILocationService locationService,
            Location? initialLocation = null)
        {
            try
            {
                
                // Tắt logging widget và FPS widget mặc định của Mapsui
                mapControl.Map.Widgets.Clear();

                // Add OSM tile layer once per MapControl (check by layer name)
                if (!mapControl.Map.Layers.Any(l => l.Name == OsmLayerName))
                {
                    var cacheDir = Path.Combine(FileSystem.CacheDirectory, "osm_tiles");
                    Directory.CreateDirectory(cacheDir);

                    var tileSource = new HttpTileSource(
                        new GlobalSphericalMercator(),
                        "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
                        name: "OpenStreetMap",
                        persistentCache: new BruTile.Cache.FileCache(cacheDir, "png"),
                        attribution: new BruTile.Attribution("© OpenStreetMap contributors", "https://openstreetmap.org/copyright"));

                    mapControl.Map.Layers.Add(new TileLayer(tileSource) { Name = OsmLayerName });
                }

                Location? focusLocation = initialLocation ?? await locationService.GetCurrentLocationAsync();

                if (focusLocation != null)
                {
                    NavigateTo(mapControl, focusLocation.Latitude, focusLocation.Longitude, 16);
                }

                var pois = await poiService.GetPOIsAsync();

                // Remove existing POI layer
                var existingLayer = mapControl.Map.Layers.FirstOrDefault(l => l.Name == PoiLayerName);
                if (existingLayer != null)
                    mapControl.Map.Layers.Remove(existingLayer);

                // Create POI markers
                var features = new List<PointFeature>();
                foreach (var poi in pois)
                {
                    var spherical = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);
                    var feature = new PointFeature(spherical.x, spherical.y);
                    feature["name"] = poi.Name ?? poi.restaurantId;
                    feature["address"] = poi.Address ?? "";
                    feature["id"] = poi.restaurantId;
                    feature.Styles.Add(CreateMarkerStyle(false));
                    features.Add(feature);
                }

                var poiLayer = new MemoryLayer
                {
                    Name = PoiLayerName,
                    Features = features
                };
                mapControl.Map.Layers.Add(poiLayer);

                if (focusLocation != null)
                {
                    var nearest = poiService.GetNearestPOI(focusLocation.Latitude, focusLocation.Longitude);
                    HighlightPOI(mapControl, nearest);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading map: {ex.Message}");
            }
        }

        /// <summary>
        /// Highlight the nearest POI on map and navigate to it.
        /// </summary>
        public static void HighlightPOI(MapControl mapControl, POI? nearest)
        {
            var poiLayer = mapControl.Map.Layers.FirstOrDefault(l => l.Name == PoiLayerName) as MemoryLayer;
            if (poiLayer == null) return;

            foreach (var feature in poiLayer.Features.OfType<PointFeature>())
            {
                feature.Styles.Clear();
                bool isHighlighted = nearest != null && feature["id"]?.ToString() == nearest.restaurantId;
                feature.Styles.Add(CreateMarkerStyle(isHighlighted));
            }

            if (nearest != null)
            {
                NavigateTo(mapControl, nearest.Latitude, nearest.Longitude, 18);
            }

            mapControl.Map.RefreshData();
        }

        /// <summary>
        /// Update user location marker on the map.
        /// </summary>
        public static void UpdateUserLocation(MapControl mapControl, double lat, double lon)
        {
            var existing = mapControl.Map.Layers.FirstOrDefault(l => l.Name == UserLocationLayerName);
            if (existing != null)
                mapControl.Map.Layers.Remove(existing);

            var spherical = SphericalMercator.FromLonLat(lon, lat);
            var feature = new PointFeature(spherical.x, spherical.y);
            feature.Styles.Add(new SymbolStyle
            {
                SymbolScale = 0.3,
                Fill = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(66, 133, 244)),
                Outline = new Pen(new Mapsui.Styles.Color(255, 255, 255), 3),
                SymbolType = SymbolType.Ellipse
            });

            var layer = new MemoryLayer
            {
                Name = UserLocationLayerName,
                Features = new[] { feature }
            };
            mapControl.Map.Layers.Add(layer);
        }

        private static void NavigateTo(MapControl mapControl, double lat, double lon, int zoomLevel)
        {
            var spherical = SphericalMercator.FromLonLat(lon, lat);
            // Resolution for OSM tile zoom levels: 156543.03392 / 2^zoom
            double resolution = 156543.03392 / Math.Pow(2, zoomLevel);
            mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(spherical.x, spherical.y), resolution);
        }

        private static SymbolStyle CreateMarkerStyle(bool isHighlighted)
        {
            return new SymbolStyle
            {
                SymbolScale = isHighlighted ? 0.45 : 0.35,
                Fill = new Mapsui.Styles.Brush(isHighlighted
                    ? new Mapsui.Styles.Color(255, 0, 0)       // Red for highlighted
                    : new Mapsui.Styles.Color(244, 140, 6)),    // Orange for normal
                Outline = new Pen(new Mapsui.Styles.Color(255, 255, 255), 2),
                SymbolType = SymbolType.Ellipse
            };
        }
    }
}
