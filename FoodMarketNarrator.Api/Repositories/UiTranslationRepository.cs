using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Repositories;

public class UiTranslationRepository
{
    private readonly AppDbContext _context;

    public UiTranslationRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TranslationModel>> GetByLanguageAsync(
        int languageId,
        string? entityType,
        IReadOnlyCollection<string>? entityIds,
        IReadOnlyCollection<string>? fieldNames)
    {
        IQueryable<TranslationModel> query = _context.Translation
            .AsNoTracking()
            .Where(t => t.LanguageId == languageId);

        if (!string.IsNullOrWhiteSpace(entityType))
        {
            query = query.Where(t => t.EntityType == entityType);
        }

        if (entityIds is { Count: > 0 })
        {
            query = query.Where(t => entityIds.Contains(t.EntityId));
        }

        if (fieldNames is { Count: > 0 })
        {
            query = query.Where(t => fieldNames.Contains(t.FieldName));
        }

        return await query
            .OrderBy(t => t.EntityType)
            .ThenBy(t => t.EntityId)
            .ThenBy(t => t.FieldName)
            .ToListAsync();
    }
}
