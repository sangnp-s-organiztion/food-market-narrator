using food_market_narrator.Services;

namespace food_market_narrator;

public partial class App : Application
{
    private readonly ILocationService _locationService;

    public App(ILocationService locationService)
	{
		InitializeComponent();
        _locationService = locationService;
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
