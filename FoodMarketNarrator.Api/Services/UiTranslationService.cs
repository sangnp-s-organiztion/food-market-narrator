using food_market_narrator_api.DTOs.Translation;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services;

public class UiTranslationService
{
    private static readonly HashSet<string> SupportedEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "restaurant",
        "dish"
    };

    private static readonly HashSet<string> SupportedFieldNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "name",
        "description",
        "address"
    };

    private readonly LanguageRepository _languageRepository;
    private readonly UiTranslationRepository _uiTranslationRepository;

    public UiTranslationService(
        LanguageRepository languageRepository,
        UiTranslationRepository uiTranslationRepository)
    {
        _languageRepository = languageRepository;
        _uiTranslationRepository = uiTranslationRepository;
    }

    public async Task<List<UiTranslationItemResponse>> GetUiTranslationsAsync(
        string languageCode,
        string? entityType,
        IEnumerable<string>? entityIds)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            throw new ArgumentException("languageCode is required.");
        }

        var normalizedEntityType = NormalizeEntityType(entityType);
        var normalizedEntityIds = NormalizeEntityIds(entityIds);

        var languageId = await ResolveLanguageIdAsync(languageCode);
        if (!languageId.HasValue)
        {
            return new List<UiTranslationItemResponse>();
        }

        var translationRows = await _uiTranslationRepository.GetByLanguageAsync(
            languageId.Value,
            normalizedEntityType,
            normalizedEntityIds,
            SupportedFieldNames);

        return translationRows.Select(x => new UiTranslationItemResponse
        {
            EntityType = x.EntityType,
            EntityId = x.EntityId,
            LanguageId = x.LanguageId,
            FieldName = x.FieldName,
            TranslatedText = x.TranslatedText
        }).ToList();
    }

    private static string? NormalizeEntityType(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return null;
        }

        var normalized = entityType.Trim().ToLowerInvariant();
        if (!SupportedEntityTypes.Contains(normalized))
        {
            throw new ArgumentException("entityType only supports 'restaurant' or 'dish'.");
        }

        return normalized;
    }

    private static List<string> NormalizeEntityIds(IEnumerable<string>? entityIds)
    {
        if (entityIds == null)
        {
            return new List<string>();
        }

        return entityIds
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task<int?> ResolveLanguageIdAsync(string languageCode)
    {
        var normalizedInput = languageCode.Trim().Replace('_', '-');
        var allLanguages = await _languageRepository.GetAllLanguagesAsync();

        var exact = allLanguages.FirstOrDefault(x =>
            string.Equals(x.LanguageCode, normalizedInput, StringComparison.OrdinalIgnoreCase));
        if (exact != null)
        {
            return exact.LanguageId;
        }

        var baseCode = normalizedInput.Split('-', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
        if (string.IsNullOrWhiteSpace(baseCode))
        {
            return null;
        }

        var shortCodeExact = allLanguages.FirstOrDefault(x =>
            string.Equals(x.LanguageCode, baseCode, StringComparison.OrdinalIgnoreCase));
        if (shortCodeExact != null)
        {
            return shortCodeExact.LanguageId;
        }

        var byPrefix = allLanguages.FirstOrDefault(x =>
            x.LanguageCode.StartsWith(baseCode + "-", StringComparison.OrdinalIgnoreCase));

        return byPrefix?.LanguageId;
    }
}
