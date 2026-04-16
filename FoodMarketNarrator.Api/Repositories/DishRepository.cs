using food_market_narrator_api.Models;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Repositories
{
    public class DishRepository
    {
        private readonly AppDbContext _context;

        public DishRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<List<DishModel>> GetByRestaurantIdAsync(string restaurantId, int page, int pageSize, int? languageId = null)
        {
            if (!languageId.HasValue)
            {
                return await _context.Dish
                    .Where(d => d.RestaurantId == restaurantId)
                    .Include(d => d.Image)
                    .OrderByDescending(d => d.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();
            }

            var dishTranslations = _context.Translation
                .AsNoTracking()
                .Where(t => t.EntityType == "dish"
                    && t.LanguageId == languageId.Value
                    && t.FieldName == "name");

            var query =
                from d in _context.Dish.AsNoTracking()
                where d.RestaurantId == restaurantId
                join t in dishTranslations
                    on d.DishId.ToString() equals t.EntityId into translationGroup
                from translation in translationGroup.DefaultIfEmpty()
                join img in _context.RestaurantImage.AsNoTracking()
                    on d.ImageId equals img.ImageId into imageGroup
                from image in imageGroup.DefaultIfEmpty()
                orderby d.CreatedAt descending
                select new DishModel
                {
                    DishId = d.DishId,
                    Name = translation != null && !string.IsNullOrEmpty(translation.TranslatedText)
                        ? translation.TranslatedText
                        : d.Name,
                    Price = d.Price,
                    CreatedAt = d.CreatedAt,
                    RestaurantId = d.RestaurantId,
                    IsActive = d.IsActive,
                    ImageId = d.ImageId,
                    Image = image
                };

            return await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

        }

        public async Task<int> CountAsync()
        {
            return await _context.Dish.CountAsync();
        }

        public async Task<DishModel?> GetByIdAsync(int dishId)
        {
            return await _context.Dish.FirstOrDefaultAsync(d => d.DishId == dishId);
        }

        public async Task<DishModel> CreateAsync(DishModel dish)
        {
            _context.Dish.Add(dish);
            await _context.SaveChangesAsync();
            return dish;
        }

        public async Task<bool> UpdateAsync(DishModel dish)
        {
            var existing = await GetByIdAsync(dish.DishId);
            if (existing == null)
            {
                return false;
            }

            existing.Name = dish.Name;
            existing.Price = dish.Price;
            existing.ImageId = dish.ImageId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteAsync(int dishId)
        {
            var existing = await GetByIdAsync(dishId);
            if (existing == null)
            {
                return false;
            }

            _context.Dish.Remove(existing);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
