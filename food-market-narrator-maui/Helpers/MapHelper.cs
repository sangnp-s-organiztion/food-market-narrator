using BruTile.Cache;
using BruTile.MbTiles;
using BruTile.Predefined;
using food_market_narrator.Models;
using food_market_narrator.Services;
using Mapsui;
using Mapsui.Layers;
using Mapsui.Projections;
using Mapsui.Tiling;
using Mapsui.Tiling.Layers;
using Mapsui.UI.Maui;
using Microsoft.Maui.Devices.Sensors;
using SQLite;

namespace food_market_narrator.Helpers;

public static class MapHelper
{
    private const string OfflineMapAssetName = "vietnam.mbtiles";

    public static async Task LoadMapAsync(
        MapView mapView,
        POIService poiService,
        ILocationService locationService,
        Location? initialLocation = null)
    {
        try
        {
            mapView.Map ??= new Mapsui.Map();

            if (!HasBaseLayer(mapView.Map))
            {
                var baseLayer = await CreateBaseLayerAsync();
                mapView.Map.Layers.Add(baseLayer);
            }

            Location? focusLocation = initialLocation ?? await locationService.GetCurrentLocationAsync();
            if (focusLocation != null)
            {
                NavigateTo(mapView, focusLocation.Latitude, focusLocation.Longitude, 1200);
            }

            var pois = await poiService.GetPOIsAsync();
            SetPoiLayer(mapView, pois, null);

            if (focusLocation != null)
            {
                var nearest = poiService.GetNearestPOI(focusLocation.Latitude, focusLocation.Longitude);
                poiService.HighlightNearestPOI(mapView, nearest);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading map: {ex.Message}");
        }
    }

    public static void SetPoiLayer(MapView mapView, IEnumerable<POI> pois, string? highlightedPoiId)
    {
        if (mapView.Map == null)
        {
            return;
        }

        mapView.Pins.Clear();

        foreach (var poi in pois)
        {
            var isHighlighted = string.Equals(poi.restaurantId, highlightedPoiId, StringComparison.OrdinalIgnoreCase);

            var pin = new Pin
            {
                Position = new Position(poi.Latitude, poi.Longitude),
                Label = poi.Name,
                Address = poi.Address,
                Color = isHighlighted ? Colors.Red : Colors.Orange,
                Scale = isHighlighted ? 0.95f : 0.8f,
                Tag = poi.restaurantId
            };

            mapView.Pins.Add(pin);
        }

        mapView.ForceUpdate();
    }

    public static void NavigateTo(MapView mapView, double latitude, double longitude, double resolution = 800)
    {
        if (mapView.Map == null)
        {
            return;
        }

        var position = SphericalMercator.FromLonLat(longitude, latitude);
        var mapPoint = new MPoint(position.x, position.y);
        mapView.Map.Navigator?.CenterOnAndZoomTo(mapPoint, resolution);
    }

    private static bool HasBaseLayer(Mapsui.Map map)
    {
        return map.Layers.Any(layer => layer.Name == "OFFLINE_MBTILES" || layer.Name == "OPENSTREETMAP_ONLINE");
    }

    private static async Task<ILayer> CreateBaseLayerAsync()
    {
        try
        {
            var offlineLayer = await TryCreateOfflineLayerAsync();
            if (offlineLayer != null)
            {
                return offlineLayer;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Offline map unavailable, falling back to online: {ex.Message}");
        }

        var onlineLayer = CreateCachedOsmLayer();
        onlineLayer.Name = "OPENSTREETMAP_ONLINE";
        return onlineLayer;
    }

    private static TileLayer CreateCachedOsmLayer()
    {
        var cacheDir = Path.Combine(FileSystem.Current.AppDataDirectory, "tilecache");
        var fileCache = new FileCache(cacheDir, "png");
        var tileSource = KnownTileSources.Create(
            KnownTileSource.OpenStreetMap,
            persistentCache: fileCache,
            userAgent: "food-market-narrator-maui");
        return new TileLayer(tileSource);
    }

    private static async Task<ILayer?> TryCreateOfflineLayerAsync()
    {
        // Check for file BEFORE referencing any MBTiles types
        // to prevent Android JIT/AOT from loading BruTile.MbTiles assembly unnecessarily
        var localPath = Path.Combine(FileSystem.Current.AppDataDirectory, OfflineMapAssetName);

        if (!File.Exists(localPath))
        {
            try
            {
                using var input = await FileSystem.Current.OpenAppPackageFileAsync(OfflineMapAssetName);
                await using var output = File.Create(localPath);
                await input.CopyToAsync(output);
            }
            catch
            {
                // No offline map asset bundled in app package
                return null;
            }
        }

        // Isolated into separate method to avoid JIT loading MBTiles types
        // when this method is compiled (VTable setup failure on Android)
        return CreateMbTilesLayer(localPath);
    }

    [System.Runtime.CompilerServices.MethodImpl(
        System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static ILayer CreateMbTilesLayer(string localPath)
    {
        var sqliteConnection = new SQLiteConnectionString(localPath, false);
        var tileSource = new MbTilesTileSource(sqliteConnection);
        return new TileLayer(tileSource) { Name = "OFFLINE_MBTILES" };
    }
}
