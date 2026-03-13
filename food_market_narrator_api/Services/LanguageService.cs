using food_market_narrator_api.DTOs.Language;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class LanguageService
    {
        private readonly LanguageRepository _languageRepository;

        public LanguageService(LanguageRepository languageRepository)
        {
            _languageRepository = languageRepository;
        }

        public async Task<LanguageResponse?> GetLanguageByCodeAsync(string languageCode)
        {
            var languageModel = await _languageRepository.GetLanguageByCodeAsync(languageCode);

            if (languageModel == null)
                return null;

            return new LanguageResponse
            {
                LanguageCode = languageModel.LanguageCode,
                LanguageName = languageModel.LanguageName
            };
        }
    }
}

