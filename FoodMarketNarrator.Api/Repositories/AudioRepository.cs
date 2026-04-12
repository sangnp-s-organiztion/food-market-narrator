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
            return await _context.Audio
                .Include(a => a.Language)
                .FirstOrDefaultAsync(a => a.AudioId == audioId);
        }

        public async Task<int> GetLatestVersionAsync(string restaurantId, int languageId)
        {
            var maxVersion = await _context.Audio
                .Where(a => a.RestaurantId == restaurantId && a.LanguageId == languageId)
                .MaxAsync(a => (int?)a.Version);

            return maxVersion ?? 0;
        }

        public async Task<AudioModel> CreateAsync(AudioModel audio)
        {
            _context.Audio.Add(audio);
            await _context.SaveChangesAsync();

            if (audio.IsActive)
            {
                var others = await _context.Audio
                    .Where(a =>
                        a.RestaurantId == audio.RestaurantId
                        && a.LanguageId == audio.LanguageId
                        && a.AudioId != audio.AudioId
                        && a.IsActive)
                    .ToListAsync();

                if (others.Count > 0)
                {
                    foreach (var other in others)
                    {
                        other.IsActive = false;
                    }

                    await _context.SaveChangesAsync();
                }
            }

            return audio;
        }

        public async Task<bool> UpdateActiveAsync(int audioId, bool isActive)
        {
            var existing = await GetByIdAsync(audioId);
            if (existing == null)
            {
                return false;
            }

            if (isActive)
            {
                var others = await _context.Audio
                    .Where(a =>
                        a.RestaurantId == existing.RestaurantId
                        && a.LanguageId == existing.LanguageId
                        && a.AudioId != existing.AudioId
                        && a.IsActive)
                    .ToListAsync();

                foreach (var other in others)
                {
                    other.IsActive = false;
                }
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

            string restaurantId = existing.RestaurantId;
            int languageId = existing.LanguageId;

            _context.Audio.Remove(existing);
            await _context.SaveChangesAsync();

            var remainingInLanguage = await _context.Audio
                .Where(a => a.RestaurantId == restaurantId && a.LanguageId == languageId)
                .ToListAsync();

            if (remainingInLanguage.Count == 1 && !remainingInLanguage[0].IsActive)
            {
                remainingInLanguage[0].IsActive = true;
                await _context.SaveChangesAsync();
            }

            return true;
        }
    }
}