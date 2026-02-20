using System.Globalization;
using food_market_narrator.Resources;
using food_market_narrator.Resources.Localization;
namespace food_market_narrator;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
