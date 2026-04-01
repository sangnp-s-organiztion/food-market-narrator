using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;


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

        public async Task<int> CountAsync()
        {
            return await _context.Restaurant.CountAsync();
        }

        public async Task<RestaurantModel> GetByIdAsync(string id)
        {
            return await _context.Restaurant
                .Include(r => r.ImageURL)
                .Include(r => r.AudioURL)
                    .ThenInclude(a => a.Language)
                .FirstOrDefaultAsync(r => r.RestaurantId == id);
        }

        public async Task<List<RestaurantModel>> GetByUserIdAsync(int userId)
        {
            return await _context.Restaurant
                .Where(r => r.UserId == userId)
                .Include(r => r.ImageURL)
                .Include(r => r.AudioURL)
                    .ThenInclude(a => a.Language)
                .ToListAsync();
        }

        public async Task<bool> UpdateAsync(RestaurantModel restaurant)
        {
            var existing = await _context.Restaurant.FirstOrDefaultAsync(r => r.RestaurantId == restaurant.RestaurantId);
            if (existing == null)
            {
                return false;
            }

            existing.Name = restaurant.Name;
            existing.Description = restaurant.Description;
            existing.Phone = restaurant.Phone;
            existing.Address = restaurant.Address;
            existing.Latitude = restaurant.Latitude;
            existing.Longitude = restaurant.Longitude;
            existing.OpenTime = restaurant.OpenTime;
            existing.CloseTime = restaurant.CloseTime;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStatusAsync(string restaurantId, bool isActive)
        {
            var existing = await _context.Restaurant.FirstOrDefaultAsync(r => r.RestaurantId == restaurantId);
            if (existing == null)
            {
                return false;
            }

            existing.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<RestaurantImageModel>> GetImagesByRestaurantIdAsync(string restaurantId)
        {
            return await _context.RestaurantImage
                .Where(i => i.RestaurantId == restaurantId)
                .OrderBy(i => i.SortOrder)
                .ToListAsync();
        }

        public async Task<RestaurantImageModel?> GetImageByIdAsync(int imageId)
        {
            return await _context.RestaurantImage.FirstOrDefaultAsync(i => i.ImageId == imageId);
        }

        public async Task<RestaurantImageModel> AddImageAsync(RestaurantImageModel image)
        {
            if (image.IsPrimary)
            {
                var oldPrimaryImages = await _context.RestaurantImage
                    .Where(i => i.RestaurantId == image.RestaurantId && i.IsPrimary)
                    .ToListAsync();

                foreach (var old in oldPrimaryImages)
                {
                    old.IsPrimary = false;
                }
            }

            _context.RestaurantImage.Add(image);
            await _context.SaveChangesAsync();
            return image;
        }

        public async Task<bool> DeleteImageAsync(int imageId)
        {
            var image = await GetImageByIdAsync(imageId);
            if (image == null)
            {
                return false;
            }

            // Break FK references from dishes before deleting the image record.
            var referencingDishes = await _context.Dish
                .Where(d => d.ImageId == imageId)
                .ToListAsync();

            foreach (var dish in referencingDishes)
            {
                dish.ImageId = null;
            }

            _context.RestaurantImage.Remove(image);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetPrimaryImageAsync(int imageId, bool isPrimary)
        {
            var image = await GetImageByIdAsync(imageId);
            if (image == null)
            {
                return false;
            }

            if (isPrimary)
            {
                var oldPrimaryImages = await _context.RestaurantImage
                    .Where(i => i.RestaurantId == image.RestaurantId && i.IsPrimary)
                    .ToListAsync();

                foreach (var old in oldPrimaryImages)
                {
                    old.IsPrimary = false;
                }
            }

            image.IsPrimary = isPrimary;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReorderImagesAsync(string restaurantId, List<(int imageId, int sortOrder)> items)
        {
            var images = await _context.RestaurantImage
                .Where(i => i.RestaurantId == restaurantId)
                .ToListAsync();

            if (!images.Any())
            {
                return false;
            }

            var imageMap = images.ToDictionary(i => i.ImageId, i => i);
            foreach (var (imageId, sortOrder) in items)
            {
                if (imageMap.TryGetValue(imageId, out var image))
                {
                    image.SortOrder = sortOrder;
                }
            }

            await _context.SaveChangesAsync();
            return true;
        }
    }
}
