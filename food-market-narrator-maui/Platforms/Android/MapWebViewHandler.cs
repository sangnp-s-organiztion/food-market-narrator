#if ANDROID
using Android.Webkit;
using AndroidX.WebKit;
using Microsoft.Maui.Handlers;

namespace food_market_narrator;

/// <summary>
/// Custom Android WebView handler for <see cref="Controls.MapWebView"/>.
/// Uses WebViewAssetLoader to serve local assets via https://appassets.androidplatform.net/
/// which works reliably on all Android versions (no file:// restrictions).
/// </summary>
public class MapWebViewHandler : WebViewHandler
{
    protected override void ConnectHandler(Android.Webkit.WebView platformView)
    {
        base.ConnectHandler(platformView);

        var assetLoader = new WebViewAssetLoader.Builder()
            .AddPathHandler("/assets/", new WebViewAssetLoader.AssetsPathHandler(Platform.CurrentActivity!))
            .Build();

        platformView.SetWebViewClient(new LocalAssetWebViewClient(assetLoader, this));

        // Allow http://127.0.0.1 tile-server requests from an https:// origin.
        platformView.Settings!.MixedContentMode = MixedContentHandling.AlwaysAllow;
    }

    private class LocalAssetWebViewClient : WebViewClient
    {
        private readonly WebViewAssetLoader _assetLoader;
        private readonly MapWebViewHandler _handler;

        public LocalAssetWebViewClient(WebViewAssetLoader assetLoader, MapWebViewHandler handler)
        {
            _assetLoader = assetLoader;
            _handler = handler;
        }

        public override bool ShouldOverrideUrlLoading(Android.Webkit.WebView? view, IWebResourceRequest? request)
        {
            var url = request?.Url?.ToString();
            if (url != null && url.StartsWith("maui://", StringComparison.OrdinalIgnoreCase))
            {
                if (_handler.VirtualView is Controls.MapWebView mapWebView)
                    mapWebView.HandleBridgeUrl(url);
                return true;
            }
            return base.ShouldOverrideUrlLoading(view, request);
        }

        public override WebResourceResponse? ShouldInterceptRequest(Android.Webkit.WebView? view, IWebResourceRequest? request)
        {
            if (request?.Url != null)
            {
                var response = _assetLoader.ShouldInterceptRequest(request.Url);
                if (response != null)
                    return response;
            }
            return base.ShouldInterceptRequest(view, request);
        }
    }
}
#endif
