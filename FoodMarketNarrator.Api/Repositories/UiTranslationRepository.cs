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

    public async Task UpsertAsync(
        string entityType,
        string entityId,
        int languageId,
        string fieldName,
        string translatedText)
    {
        var normalizedEntityType = (entityType ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedEntityId = (entityId ?? string.Empty).Trim();
        var normalizedFieldName = (fieldName ?? string.Empty).Trim().ToLowerInvariant();

        var existing = await _context.Translation.FirstOrDefaultAsync(t =>
            t.EntityType == normalizedEntityType
            && t.EntityId == normalizedEntityId
            && t.LanguageId == languageId
            && t.FieldName == normalizedFieldName);

        if (existing == null)
        {
            _context.Translation.Add(new TranslationModel
            {
                EntityType = normalizedEntityType,
                EntityId = normalizedEntityId,
                LanguageId = languageId,
                FieldName = normalizedFieldName,
                TranslatedText = translatedText.Trim(),
                CreatedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.TranslatedText = translatedText.Trim();
            existing.CreatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task DeleteByEntityFieldAndLanguageAsync(
        string entityType,
        string entityId,
        string fieldName,
        int languageId)
    {
        var normalizedEntityType = (entityType ?? string.Empty).Trim().ToLowerInvariant();
        var normalizedEntityId = (entityId ?? string.Empty).Trim();
        var normalizedFieldName = (fieldName ?? string.Empty).Trim().ToLowerInvariant();

        var existing = await _context.Translation.Where(t =>
            t.EntityType == normalizedEntityType
            && t.EntityId == normalizedEntityId
            && t.FieldName == normalizedFieldName
            && t.LanguageId == languageId).ToListAsync();

        if (existing.Count == 0)
        {
            return;
        }

        _context.Translation.RemoveRange(existing);
        await _context.SaveChangesAsync();
    }

}
