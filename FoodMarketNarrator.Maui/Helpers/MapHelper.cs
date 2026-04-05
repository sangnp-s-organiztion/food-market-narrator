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
using Microsoft.Maui.Devices.Sensors;

namespace food_market_narrator.Helpers
{
    public static class MapHelper
    {
        private const string PoiLayerName = "POIs";
        private const string PoiLabelLayerName = "POILabels";
        private const string UserLocationLayerName = "UserLocation";
        private const string OsmLayerName = "OpenStreetMap";
        private const double PoiLabelOffsetMeters = 8;
        private const double PoiLabelVisibleRadiusMeters = 40;

        public static async Task LoadMapAsync(
            MapControl mapControl,
            IPOIService poiService,
            ILocationService locationService,
            Location? initialLocation = null,
            int initialZoomLevel = 16)
        {
            try
            {
                mapControl.Map.Widgets.Clear();

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
                    NavigateTo(mapControl, focusLocation.Latitude, focusLocation.Longitude, initialZoomLevel);
                    UpdateUserLocation(mapControl, focusLocation.Latitude, focusLocation.Longitude);
                }

                var pois = await poiService.GetPOIsAsync();

                var existingLayer = mapControl.Map.Layers.FirstOrDefault(l => l.Name == PoiLayerName);
                if (existingLayer != null)
                    mapControl.Map.Layers.Remove(existingLayer);

                var existingLabelLayer = mapControl.Map.Layers.FirstOrDefault(l => l.Name == PoiLabelLayerName);
                if (existingLabelLayer != null)
                    mapControl.Map.Layers.Remove(existingLabelLayer);

                var features = new List<PointFeature>();
                foreach (var poi in pois)
                {
                    var spherical = SphericalMercator.FromLonLat(poi.Longitude, poi.Latitude);
                    var feature = new PointFeature(spherical.x, spherical.y);
                    feature["name"] = poi.Name ?? poi.restaurantId;
                    feature["address"] = poi.Address ?? "";
                    feature["id"] = poi.restaurantId;
                    feature["lat"] = poi.Latitude;
                    feature["lon"] = poi.Longitude;
                    feature.Styles.Add(CreateMarkerStyle(false));
                    features.Add(feature);
                }

                var poiLayer = new MemoryLayer
                {
                    Name = PoiLayerName,
                    Features = features
                };
                mapControl.Map.Layers.Add(poiLayer);

                var poiLabelLayer = new MemoryLayer
                {
                    Name = PoiLabelLayerName,
                    Features = new List<IFeature>(),
                    Style = CreatePoiLabelStyle()
                };
                mapControl.Map.Layers.Add(poiLabelLayer);

                if (focusLocation != null)
                {
                    RefreshPoiLabelsByUserLocation(mapControl, focusLocation.Latitude, focusLocation.Longitude);
                }

                if (focusLocation != null)
                {
                    var nearest = poiService.GetNearestPOI(focusLocation.Latitude, focusLocation.Longitude);
                    HighlightPOI(mapControl, nearest);
                }
            }
            catch (Exception)
            {
                // Console.WriteLine($"Error loading map: {ex.Message}");
            }
        }

        /// <summary>
        /// Highlight the nearest POI on map without moving camera.
        /// </summary>
        public static void HighlightPOI(MapControl mapControl, POI? nearest, bool isSearchResult = false)
        {
            var ids = nearest?.restaurantId == null
                ? null
                : new[] { nearest.restaurantId };
            HighlightPOIs(mapControl, ids, isSearchResult);
        }

        public static void HighlightPOIs(MapControl mapControl, IEnumerable<string>? highlightedPoiIds, bool isSearchResult = false)
        {
            var poiLayer = mapControl.Map.Layers.FirstOrDefault(l => l.Name == PoiLayerName) as MemoryLayer;
            if (poiLayer == null) return;

            var idSet = highlightedPoiIds == null
                ? new HashSet<string>()
                : highlightedPoiIds
                    .Where(id => !string.IsNullOrWhiteSpace(id))
                    .ToHashSet(StringComparer.Ordinal);

            var highlightedFeatures = new List<PointFeature>();

            foreach (var feature in poiLayer.Features.OfType<PointFeature>())
            {
                feature.Styles.Clear();
                var featureId = feature["id"]?.ToString();
                bool isHighlighted = featureId != null && idSet.Contains(featureId);
                feature.Styles.Add(CreateMarkerStyle(isHighlighted, isSearchResult && isHighlighted));
                if (isHighlighted)
                {
                    highlightedFeatures.Add(feature);
                }
            }

            if (highlightedFeatures.Count > 0)
            {
                var reordered = poiLayer.Features
                    .OfType<PointFeature>()
                    .Where(f => !highlightedFeatures.Contains(f))
                    .Cast<IFeature>()
                    .ToList();

                reordered.AddRange(highlightedFeatures);
                poiLayer.Features = reordered;
            }

            // FIX: Invalidate layer data cache trước, sau đó force re-render graphics.
            // RefreshData() đơn độc không đủ khi 2 POI gần nhau vì Mapsui
            // cache viewport và không detect style change nếu bounding box không đổi.
            poiLayer.DataHasChanged();
            mapControl.Map.RefreshData();
            mapControl.Map.RefreshGraphics();
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

            RefreshPoiLabelsByUserLocation(mapControl, lat, lon);

            // Tương tự: force re-render sau khi update user location
            mapControl.Map.RefreshData();
            mapControl.Map.RefreshGraphics();
        }

        private static void RefreshPoiLabelsByUserLocation(MapControl mapControl, double userLat, double userLon)
        {
            var poiLayer = mapControl.Map.Layers.FirstOrDefault(l => l.Name == PoiLayerName) as MemoryLayer;
            var poiLabelLayer = mapControl.Map.Layers.FirstOrDefault(l => l.Name == PoiLabelLayerName) as MemoryLayer;
            if (poiLayer == null || poiLabelLayer == null)
            {
                return;
            }

            var labelFeatures = new List<IFeature>();
            foreach (var poiFeature in poiLayer.Features.OfType<PointFeature>())
            {
                if (!TryGetCoordinate(poiFeature["lat"], out var poiLat)
                    || !TryGetCoordinate(poiFeature["lon"], out var poiLon))
                {
                    continue;
                }

                var distanceMeters = Location.CalculateDistance(
                    new Location(userLat, userLon),
                    new Location(poiLat, poiLon),
                    DistanceUnits.Kilometers) * 1000;

                if (distanceMeters > PoiLabelVisibleRadiusMeters)
                {
                    continue;
                }

                var poiSpherical = SphericalMercator.FromLonLat(poiLon, poiLat);
                var userSpherical = SphericalMercator.FromLonLat(userLon, userLat);
                var deltaX = poiSpherical.x - userSpherical.x;
                var deltaY = poiSpherical.y - userSpherical.y;

                var labelX = poiSpherical.x;
                var labelY = poiSpherical.y;

                if (Math.Abs(deltaX) >= Math.Abs(deltaY))
                {
                    labelX += deltaX >= 0 ? PoiLabelOffsetMeters : -PoiLabelOffsetMeters;
                }
                else
                {
                    labelY += deltaY >= 0 ? PoiLabelOffsetMeters : -PoiLabelOffsetMeters;
                }

                var labelFeature = new PointFeature(labelX, labelY);
                var rawName = poiFeature["name"]?.ToString() ?? string.Empty;
                labelFeature["name"] = $"  {rawName}  ";
                labelFeature["id"] = poiFeature["id"]?.ToString() ?? string.Empty;
                labelFeatures.Add(labelFeature);
            }

            poiLabelLayer.Features = labelFeatures;
            poiLabelLayer.DataHasChanged();
        }

        private static bool TryGetCoordinate(object? value, out double coordinate)
        {
            coordinate = 0;

            switch (value)
            {
                case double d:
                    coordinate = d;
                    return true;
                case float f:
                    coordinate = f;
                    return true;
                case decimal m:
                    coordinate = (double)m;
                    return true;
                case string s when double.TryParse(s, out var parsed):
                    coordinate = parsed;
                    return true;
                default:
                    return false;
            }
        }

        public static void CenterOnUserLocation(MapControl mapControl, double lat, double lon, int? zoomLevel = null)
        {
            if (mapControl?.Map?.Navigator == null)
            {
                return;
            }

            try
            {
                var spherical = SphericalMercator.FromLonLat(lon, lat);
                var center = new MPoint(spherical.x, spherical.y);

                var viewport = mapControl.Map.Navigator.Viewport;
                var fallbackResolution = 156543.03392 / Math.Pow(2, 19);
                var viewportResolution = viewport.Resolution;

                var resolution = zoomLevel.HasValue
                    ? 156543.03392 / Math.Pow(2, zoomLevel.Value)
                    : viewportResolution;

                if (double.IsNaN(resolution) || double.IsInfinity(resolution) || resolution <= 0)
                {
                    resolution = fallbackResolution;
                }

                mapControl.Map.Navigator.CenterOnAndZoomTo(center, resolution);
                mapControl.Map.RefreshGraphics();
            }
            catch (Exception)
            {
                // Ignore transient map navigation failures and wait for next location tick.
            }
        }

        private static void NavigateTo(MapControl mapControl, double lat, double lon, int zoomLevel)
        {
            var spherical = SphericalMercator.FromLonLat(lon, lat);
            double resolution = 156543.03392 / Math.Pow(2, zoomLevel);
            mapControl.Map.Navigator.CenterOnAndZoomTo(new MPoint(spherical.x, spherical.y), resolution);
        }

        private static SymbolStyle CreateMarkerStyle(bool isHighlighted, bool isSearchHighlight = false)
        {
            var highlightColor = isSearchHighlight
                ? new Mapsui.Styles.Color(0, 170, 145)
                : new Mapsui.Styles.Color(255, 0, 0);

            return new SymbolStyle
            {
                SymbolScale = isHighlighted ? (isSearchHighlight ? 0.5 : 0.45) : 0.35,
                Fill = new Mapsui.Styles.Brush(isHighlighted
                    ? highlightColor
                    : new Mapsui.Styles.Color(244, 140, 6)),
                Outline = new Pen(new Mapsui.Styles.Color(255, 255, 255), 2),
                SymbolType = SymbolType.Ellipse
            };
        }

        private static LabelStyle CreatePoiLabelStyle()
        {
            return new LabelStyle
            {
                LabelColumn = "name",
                ForeColor = new Mapsui.Styles.Color(47, 54, 64),
                BackColor = new Mapsui.Styles.Brush(new Mapsui.Styles.Color(255, 255, 255, 232)),
                CollisionDetection = true
            };
        }
    }
}

