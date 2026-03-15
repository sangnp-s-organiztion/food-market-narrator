using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;
using food_market_narrator_api.DTOs.Restaurant;


namespace food_market_narrator_api.Repositories
{
    public class RestaurantRepository
    {
        private readonly AppDbContext _context;
        public RestaurantRepository(AppDbContext context)
        {
            _context = context;
        }
        public async Task<List<RestaurantModel>> GetAllAsync()
        {
            return await _context.Restaurant
                .Include(r => r.ImageURL)
                .Include(r => r.AudioURL)
                    .ThenInclude(a => a.Language)
                .ToListAsync();
        }

        public async Task<RestaurantModel> GetByIdAsync(string id)
        {
            return await _context.Restaurant
                .Include(r => r.ImageURL)
                .Include(r => r.AudioURL)
                    .ThenInclude(a => a.Language)
                .FirstOrDefaultAsync(r => r.RestaurantId == id);
        }

        public async Task<List<RestaurantModel>> GetByIdsAsync(List<string> ids)
        {
            return await _context.Restaurant
                .Where(r => ids.Contains(r.RestaurantId))
                .Include(r => r.ImageURL)
                .Include(r => r.AudioURL)
                    .ThenInclude(a => a.Language)
                .ToListAsync();
        }

        public async Task<List<RestaurantModel>> GetByUserIdAsync(int userId)
        {
            return await _context.Restaurant
                .Where(r => r.UserId == userId)
                .Include(r => r.ImageURL)
                .Include(r => r.AudioURL)
                    .ThenInclude(a => a.Language)
                .OrderBy(r => r.RestaurantId)
                .ToListAsync();
        }

        public async Task<RestaurantModel?> UpdateAsync(string id, DTOs.Restaurant.RestaurantRequestDto dto)
        {
            var restaurant = await GetByIdAsync(id);
            if (restaurant == null) return null;

            restaurant.Name = dto.Name;
            restaurant.Description = dto.Description;
            restaurant.Latitude = dto.Latitude;
            restaurant.Longitude = dto.Longitude;
            restaurant.Address = dto.Address;
            restaurant.Phone = dto.Phone;
            restaurant.OpenTime = dto.OpenTime;
            restaurant.CloseTime = dto.CloseTime;
            restaurant.IsActive = dto.IsActive;

            // Replace images: remove existing and add new
            var existingImages = _context.RestaurantImage.Where(i => i.RestaurantId == id);
            _context.RestaurantImage.RemoveRange(existingImages);
            if (dto.Images != null)
            {
                foreach (var img in dto.Images)
                {
                    _context.RestaurantImage.Add(new RestaurantImageModel
                    {
                        RestaurantId = id,
                        ImageUrl = img.ImageUrl,
                        IsPrimary = img.IsPrimary,
                        SortOrder = img.SortOrder
                    });
                }
            }

            // Replace audios similarly
            var existingAudios = _context.Audio.Where(a => a.RestaurantId == id);
            _context.Audio.RemoveRange(existingAudios);
            if (dto.Audios != null)
            {
                foreach (var a in dto.Audios)
                {
                    _context.Audio.Add(new AudioModel
                    {
                        RestaurantId = id,
                        LanguageId = a.LanguageId,
                        AudioUrl = a.AudioUrl,
                        Version = a.Version,
                        IsActive = a.IsActive,
                        DateGeneration = DateTime.Now
                    });
                }
            }

            await _context.SaveChangesAsync();
            return await GetByIdAsync(id);
        }

        public async Task<RestaurantModel?> SetActiveAsync(string id, bool isActive)
        {
            var restaurant = await GetByIdAsync(id);
            if (restaurant == null) return null;
            restaurant.IsActive = isActive;
            await _context.SaveChangesAsync();
            return restaurant;
        }
    }
}
