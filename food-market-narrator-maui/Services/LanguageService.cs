using System.Globalization;
using food_market_narrator.Resources;
using food_market_narrator.Resources.Localization;

namespace food_market_narrator.Services;

public class LanguageService
{

    private const string LANGUAGE_KEY = "AppLanguage";

    public string CurrentLanguage
    {
        get
        {
            Console.WriteLine($"CurrentLanguage getter called, returning: {Preferences.Get(LANGUAGE_KEY, "vi")}");
            return Preferences.Get(LANGUAGE_KEY, "vi"); 
            // mặc định tiếng Việt nếu chưa có
        }
    }

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


