using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Repositories
{
    public class AudioRepository
    {
        private readonly AppDbContext _context;
        public AudioRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<AudioModel>> GetAllAsync()
        {
            return await _context.Audio
                .Include(a => a.Restaurant)
                .Include(a => a.Language)
                .ToListAsync();
        }
    }
}