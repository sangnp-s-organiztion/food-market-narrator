using food_market_narrator.Services;
using System.Globalization;
using food_market_narrator.Resources;
using food_market_narrator.Resources.Localization;

namespace food_market_narrator;

public partial class App : Application
{
    private readonly ILocationService _locationService;
    private readonly NarrationFlowService _narrationFlowService;

	public App(ILocationService locationService, NarrationFlowService narrationFlowService)
	{
		InitializeComponent();
        _locationService = locationService;
        _narrationFlowService = narrationFlowService;
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}

    protected override async void OnStart()
    {
        base.OnStart();
        // Start tracking immediately when app starts
        await _locationService.StartTrackingAsync();
    }

    protected override void OnSleep()
    {
        base.OnSleep();
        // For true background tracking without Foreground Service, OS might kill this.
        // We'll leave it running to hope for the best if permission allows background (on Android).
        // If strict lifecycle management is needed, consider StopTracking() here.
    }
}
