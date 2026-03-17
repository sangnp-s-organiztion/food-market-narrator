using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Repositories;

public class LanguageRepository
{
    private readonly AppDbContext _context;
    public LanguageRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<LanguageModel?> GetLanguageByCodeAsync(string languageCode)
    {
        return await _context.Language.FirstOrDefaultAsync(l => l.LanguageCode == languageCode);
    }

    public async Task<List<LanguageModel>> GetAllLanguagesAsync()
    {
        return await _context.Language.ToListAsync();
    }
}