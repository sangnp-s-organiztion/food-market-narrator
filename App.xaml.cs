using System.Globalization;
using food_market_narrator.Resources;
namespace food_market_narrator;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
		ApplySavedLanguage();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}



	private void ApplySavedLanguage()
    {
        var savedLang = Preferences.Get("AppLanguage", "vi-VN");

        var culture = new CultureInfo(savedLang);

        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        AppResources.Culture = culture;
    }
}
