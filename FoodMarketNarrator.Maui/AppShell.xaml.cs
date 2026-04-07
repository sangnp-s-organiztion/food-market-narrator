using food_market_narrator.Views;


namespace food_market_narrator;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		Routing.RegisterRoute(nameof(POIDetailPage), typeof(POIDetailPage));
		Routing.RegisterRoute(nameof(TourPage), typeof(TourPage));
		Routing.RegisterRoute(nameof(FavoritePage), typeof(FavoritePage));
		Routing.RegisterRoute(nameof(HistoryPage), typeof(HistoryPage));
		Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
    }
}
