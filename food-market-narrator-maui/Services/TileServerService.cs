using System.Net;

namespace food_market_narrator.Services;

/// <summary>
/// Lightweight local HTTP server that serves a PMTiles tile file with HTTP Range support.
/// MapLibre GL JS + the pmtiles JS library require byte-range requests, which the Android
/// WebViewAssetLoader cannot satisfy for large files.  This server bridges that gap by
/// listening on http://127.0.0.1:8765/ and streaming requested byte ranges from disk.
///
/// ── Enabling offline tiles ───────────────────────────────────────────────────────────────
/// 1. Obtain a raster PMTiles file for your area (e.g. from https://protomaps.com/downloads
///    or https://openfreemap.org) and save it as  tiles.pmtiles.
/// 2. Push the file to the device:
///      adb push tiles.pmtiles /data/data/com.companyname.foodmarketnarrator/files/tiles.pmtiles
///    OR copy it to FileSystem.AppDataDirectory on the target device.
/// 3. The server auto-detects the file and switches the map to offline mode.
/// ────────────────────────────────────────────────────────────────────────────────────────
/// </summary>
public sealed class TileServerService : IDisposable
{
    public const int Port = 8765;

    private HttpListener? _listener;
    private CancellationTokenSource? _cts;

    // ── Tile file path ────────────────────────────────────────────────────────

    public static string GetTilesFilePath()
        => Path.Combine(FileSystem.AppDataDirectory, "tiles.pmtiles");

    public bool HasLocalTiles => File.Exists(GetTilesFilePath());

    // ── Server lifecycle ──────────────────────────────────────────────────────

    public void Start()
    {
        if (_listener?.IsListening == true) return;

        _cts = new CancellationTokenSource();
        _listener = new HttpListener();
        _listener.Prefixes.Add($"http://127.0.0.1:{Port}/");

        try
        {
            _listener.Start();
            _ = Task.Run(() => AcceptLoopAsync(_cts.Token));
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TileServer] Failed to start: {ex.Message}");
        }
    }

    // ── Request handling ──────────────────────────────────────────────────────

    private async Task AcceptLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested && (_listener?.IsListening == true))
        {
            try
            {
                var ctx = await _listener.GetContextAsync().ConfigureAwait(false);
                _ = Task.Run(() => HandleAsync(ctx, ct), ct);
            }
            catch (ObjectDisposedException) { break; }
            catch (HttpListenerException) { break; }
            catch { /* continue */ }
        }
    }

    private static async Task HandleAsync(HttpListenerContext ctx, CancellationToken ct)
    {
        try
        {
            ctx.Response.Headers.Add("Access-Control-Allow-Origin", "*");
            ctx.Response.Headers.Add("Access-Control-Allow-Headers", "Range, Content-Type");

            if (ctx.Request.HttpMethod == "OPTIONS")
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Close();
                return;
            }

            // Health-check endpoint
            if (ctx.Request.Url?.AbsolutePath == "/")
            {
                ctx.Response.StatusCode = 200;
                ctx.Response.Close();
                return;
            }

            var filePath = GetTilesFilePath();
            if (!File.Exists(filePath))
            {
                ctx.Response.StatusCode = 404;
                ctx.Response.Close();
                return;
            }

            var total = new FileInfo(filePath).Length;
            ctx.Response.ContentType = "application/octet-stream";
            ctx.Response.Headers.Add("Accept-Ranges", "bytes");

            var rangeHeader = ctx.Request.Headers["Range"];
            if (!string.IsNullOrEmpty(rangeHeader) && rangeHeader.StartsWith("bytes="))
            {
                // Byte-range response (required by pmtiles JS library)
                var parts = rangeHeader[6..].Split('-');
                long start = long.Parse(parts[0]);
                long end   = parts.Length > 1 && parts[1].Length > 0
                    ? long.Parse(parts[1])
                    : total - 1;
                end = Math.Min(end, total - 1);
                long length = end - start + 1;

                ctx.Response.StatusCode = 206;
                ctx.Response.ContentLength64 = length;
                ctx.Response.Headers.Add("Content-Range", $"bytes {start}-{end}/{total}");

                await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                fs.Seek(start, SeekOrigin.Begin);
                var buf = new byte[length];
                await fs.ReadExactlyAsync(buf, ct);
                await ctx.Response.OutputStream.WriteAsync(buf, ct);
            }
            else
            {
                // Full-file response
                ctx.Response.StatusCode = 200;
                ctx.Response.ContentLength64 = total;
                await using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
                await fs.CopyToAsync(ctx.Response.OutputStream, ct);
            }

            ctx.Response.OutputStream.Close();
        }
        catch { /* absorb connection resets */ }
    }

    // ── Dispose ───────────────────────────────────────────────────────────────

    public void Dispose()
    {
        _cts?.Cancel();
        try { _listener?.Stop(); } catch { }
        _listener?.Close();
        _cts?.Dispose();
    }
}
