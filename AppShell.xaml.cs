using food_market_narrator.Views;


namespace food_market_narrator;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();
		
		Routing.RegisterRoute(nameof(MapPage), typeof(MapPage));
	}
}
