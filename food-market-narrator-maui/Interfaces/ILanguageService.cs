using food_market_narrator.Models;

namespace food_market_narrator.Services;

public interface ILanguageService
{
    string CurrentLanguage { get; }
    Task<List<LanguageModel>> GetAllLanguagesAsync();
    Task<LanguageModel?> GetLanguageByCodeAsync(string languageCode);
    void ChangeLanguage(string cultureCode);
}
