using System.Globalization;
using System.Text.Json;
using food_market_narrator.Models;

namespace food_market_narrator.Controls;

/// <summary>
/// Custom WebView control wrapping a MapLibre GL JS map.
/// Provides a .NET ↔ JavaScript bridge for marker management, camera control, and POI selection.
/// </summary>
public class MapWebView : WebView
{
    // ── Events ────────────────────────────────────────────────────────────────

    /// <summary>Fires when the user taps a POI marker. Arg is the restaurantId.</summary>
    public event EventHandler<string>? POISelected;

    // ── Internal state ────────────────────────────────────────────────────────

    private TaskCompletionSource? _readySource;

    public bool IsMapReady { get; private set; }

    // ── Constructor ───────────────────────────────────────────────────────────

    public MapWebView()
    {
        Navigating += OnNavigating;
    }

    // ── Bridge: JS → .NET ────────────────────────────────────────────────────

    private void OnNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (!e.Url.StartsWith("maui://", StringComparison.OrdinalIgnoreCase))
            return;

        e.Cancel = true;
        HandleBridgeUrl(e.Url);
    }

    /// <summary>
    /// Process a maui:// bridge URL. Called from Navigating event or platform handler.
    /// </summary>
    internal void HandleBridgeUrl(string url)
    {
        if (!url.StartsWith("maui://", StringComparison.OrdinalIgnoreCase))
            return;

        var questionIdx = url.IndexOf('?');
        var action = questionIdx >= 0
            ? url[7..questionIdx]   // strip "maui://"
            : url[7..];

        switch (action)
        {
            case "mapReady":
                IsMapReady = true;
                _readySource?.TrySetResult();
                break;

            case "poiSelected":
                if (questionIdx >= 0)
                {
                    var rawId = GetQueryParam(url[(questionIdx + 1)..], "data");
                    if (rawId != null)
                        POISelected?.Invoke(this, rawId);
                }
                break;
        }
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Load the MapLibre map. Pass <paramref name="useLocalTiles"/>=true when a local
    /// PMTiles tile-server is running on <paramref name="tileServerPort"/>.
    /// </summary>
    public void LoadMap(bool useLocalTiles = false, int tileServerPort = 8765)
    {
        IsMapReady = false;
        _readySource = new TaskCompletionSource();

        var query = useLocalTiles ? $"?offline=1&port={tileServerPort}" : "";

#if ANDROID
        // WebViewAssetLoader serves APK assets via a virtual https:// URL.
        // Works on all Android versions without file:// restrictions.
        Source = new UrlWebViewSource
        {
            Url = $"file:///android_asset/map.html{query}"
        };
#elif IOS || MACCATALYST
        var bundlePath = Foundation.NSBundle.MainBundle.BundlePath;
        Source = new UrlWebViewSource
        {
            Url = $"file://{bundlePath}/map.html{query}"
        };
#else
        // Windows: load file relative to executable
        var localPath = Path.Combine(AppContext.BaseDirectory, "map.html");
        Source = new UrlWebViewSource { Url = $"file:///{localPath.Replace('\\', '/')}{query}" };
#endif
    }

    /// <summary>
    /// Await map initialisation. Call immediately after <see cref="LoadMap"/>.
    /// </summary>
    public Task WhenMapReadyAsync(CancellationToken ct = default)
    {
        if (IsMapReady) return Task.CompletedTask;

        _readySource ??= new TaskCompletionSource();
        ct.Register(() => _readySource.TrySetCanceled(ct));
        return _readySource.Task;
    }

    // ── Bridge: .NET → JS ────────────────────────────────────────────────────

    public Task MoveToLocationAsync(double lat, double lng, double zoom = 15)
        => EvaluateJavaScriptAsync(
            $"setCamera({F(lat)},{F(lng)},{F(zoom)})");

    public Task AddMarkersAsync(IEnumerable<POI> pois)
    {
        var payload = pois.Select(p => new
        {
            id      = p.restaurantId,
            name    = p.Name    ?? p.restaurantId,
            address = p.Address ?? string.Empty,
            lat     = p.Latitude,
            lng     = p.Longitude
        });

        var json    = JsonSerializer.Serialize(payload);
        var escaped = json.Replace("\\", "\\\\").Replace("'", "\\'");
        return EvaluateJavaScriptAsync($"addMarkers('{escaped}')");
    }

    public Task HighlightMarkerAsync(string? restaurantId)
    {
        var id = restaurantId != null ? $"'{restaurantId}'" : "null";
        return EvaluateJavaScriptAsync($"highlightMarker({id})");
    }

    public Task UpdateUserLocationAsync(double lat, double lng)
        => EvaluateJavaScriptAsync($"updateUserLocation({F(lat)},{F(lng)})");

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Invariant-culture double → string (safe for JS injection).</summary>
    private static string F(double v) => v.ToString(CultureInfo.InvariantCulture);

    private static string? GetQueryParam(string queryString, string key)
    {
        foreach (var part in queryString.Split('&'))
        {
            var kv = part.Split('=', 2);
            if (kv.Length == 2 && kv[0] == key)
                return Uri.UnescapeDataString(kv[1]);
        }
        return null;
    }
}
