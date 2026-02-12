using Microsoft.Extensions.DependencyInjection;
using food_market_narrator.Views;
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