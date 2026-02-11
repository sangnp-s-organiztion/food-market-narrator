using Microsoft.Extensions.Logging;
using food_market_narrator.Views;
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

		// Register pages for dependency injection
		builder.Services.AddTransient<MainPage>();
		builder.Services.AddTransient<MapPage>();

#if DEBUG
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
