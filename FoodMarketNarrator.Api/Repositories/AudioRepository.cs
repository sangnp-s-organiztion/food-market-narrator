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

        public async Task<int> CountAsync()
        {
            return await _context.Audio.CountAsync();
        }

        public async Task<List<AudioModel>> GetByRestaurantIdAsync(string restaurantId)
        {
            return await _context.Audio
                .Include(a => a.Language)
                .Where(a => a.RestaurantId == restaurantId)
                .OrderByDescending(a => a.DateGeneration)
                .ToListAsync();
        }

        public async Task<AudioModel?> GetByIdAsync(int audioId)
        {
            return await _context.Audio.FirstOrDefaultAsync(a => a.AudioId == audioId);
        }

        public async Task<AudioModel> CreateAsync(AudioModel audio)
        {
            _context.Audio.Add(audio);
            await _context.SaveChangesAsync();
            return audio;
        }

        public async Task<bool> UpdateActiveAsync(int audioId, bool isActive)
        {
            var existing = await GetByIdAsync(audioId);
            if (existing == null)
            {
                return false;
            }

            existing.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int audioId)
        {
            var existing = await GetByIdAsync(audioId);
            if (existing == null)
            {
                return false;
            }

            _context.Audio.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}