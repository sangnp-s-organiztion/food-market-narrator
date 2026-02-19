using food_market_narrator.Services;
using food_market_narrator.Views;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Maps;


namespace food_market_narrator;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiMaps()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                fonts.AddFont("fa-solid-900.ttf", "FASolid");
            });


        builder.Services.AddHttpClient<POIService>(client =>
        {
            client.BaseAddress = new Uri("http://10.0.2.2:5044/");
        })
        .ConfigurePrimaryHttpMessageHandler(() =>
        {
            return new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };
        });

        // Register pages for dependency injection
        builder.Services.AddTransient<POIService>();
        builder.Services.AddTransient<NarrationFlowService>();
        builder.Services.AddTransient<MainPage>();
        builder.Services.AddTransient<MapPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        builder.ConfigureMauiHandlers(handlers =>
        {
#if ANDROID
            handlers.AddHandler<Microsoft.Maui.Controls.Maps.Map, CustomMapHandler>();
#endif
        });

        return builder.Build();
    }
}
