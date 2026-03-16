using food_market_narrator.Services;
using food_market_narrator.Settings;
using food_market_narrator.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Devices;
using SkiaSharp.Views.Maui.Controls.Hosting;


namespace food_market_narrator;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseSkiaSharp()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fa-solid-900.ttf", "FASolid");
            });

        builder.Services.AddSingleton(sp =>
        {
            Console.WriteLine(DeviceInfo.DeviceType);
            Console.WriteLine(AppSettings.ApiBaseUrl);

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            return new HttpClient(handler)
            {
                BaseAddress = new Uri(AppSettings.ApiBaseUrl)
            };
        });

        // Register pages for dependency injection
        builder.Services.AddSingleton<IPOIService, POIService>(); // POI data cache should be singleton
        builder.Services.AddSingleton<IAudioService, AudioService>();
        builder.Services.AddSingleton<ILanguageService, LanguageService>();
        builder.Services.AddSingleton<NarrationFlowService>(); // Must be singleton to track played POIs
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapPage>();
        builder.Services.AddSingleton<ILocationService, LocationService>(); // Updated class name to singular

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}

