namespace food_market_narrator.Services;

public interface ILanguageService
{
    string CurrentLanguage { get; }
    void ChangeLanguage(string cultureCode);
}
