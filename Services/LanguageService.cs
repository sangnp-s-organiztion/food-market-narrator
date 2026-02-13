using System.Globalization;
using food_market_narrator.Resources;

namespace food_market_narrator.Services;

public class LanguageService
{
	// Thay đổi ngôn ngữ ứng dụng
	public void ChangeLanguage(string cultureCode)
    {
		// Lưu lại để lần sau app mở tự load
        Preferences.Set("AppLanguage", cultureCode);

        var culture = new CultureInfo(cultureCode);

        Thread.CurrentThread.CurrentUICulture = culture;
        Thread.CurrentThread.CurrentCulture = culture;

        AppResources.Culture = culture;

        // Reload AppShell (nhẹ hơn recreate toàn app)
        Application.Current.Windows[0].Page = new AppShell();

    }
}


