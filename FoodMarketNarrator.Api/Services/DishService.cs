using food_market_narrator_api.DTOs.Dish;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class DishService
    {
        private readonly DishRepository _dishRepository;
        private readonly LanguageRepository _languageRepository;

        public DishService(DishRepository dishRepository, LanguageRepository languageRepository)
        {
            _dishRepository = dishRepository;
            _languageRepository = languageRepository;
        }

        public async Task<List<DishResponse>> GetByRestaurantIdAsync(string restaurantId, int page, int pageSize, string? languageCode = null)
        {
            var languageId = await ResolveLanguageIdAsync(languageCode);
            var dishes = await _dishRepository.GetByRestaurantIdAsync(restaurantId, page, pageSize, languageId);
            return dishes.Select(Map).ToList();
        }

        private async Task<int?> ResolveLanguageIdAsync(string? languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                return null;
            }

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

        public async Task<int> CountDishesAsync()
        {
            return await _dishRepository.CountAsync();
        }

        public async Task<DishResponse?> CreateAsync(string restaurantId, CreateDishRequest request)
        {
            var dish = new DishModel
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                RestaurantId = restaurantId,
                ImageId = request.ImageId,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _dishRepository.CreateAsync(dish);
            return Map(created);
        }

        public async Task<DishResponse?> UpdateAsync(int dishId, UpdateDishRequest request)
        {
            var existing = await _dishRepository.GetByIdAsync(dishId);
            if (existing == null)
            {
                return null;
            }

            existing.Name = request.Name.Trim();
            existing.Price = request.Price;
            existing.ImageId = request.ImageId;

            bool updated = await _dishRepository.UpdateAsync(existing);
            if (!updated)
            {
                return null;
            }

            return Map(existing);
        }

        public async Task<bool> DeleteAsync(int dishId)
        {
            return await _dishRepository.DeleteAsync(dishId);
        }

        private static DishResponse Map(DishModel dish)
        {
            // Extract filename từ ImageUrl (ví dụ: /maui-images/dish_1.jpg -> dish_1.jpg)
            string? imageFileName = null;
            if (!string.IsNullOrWhiteSpace(dish.Image?.ImageUrl))
            {
                imageFileName = Path.GetFileName(dish.Image.ImageUrl);
            }

            return new DishResponse
            {
                DishId = dish.DishId,
                Name = dish.Name,
                Price = dish.Price,
                RestaurantId = dish.RestaurantId,
                ImageId = dish.ImageId,
                ImageFileName = imageFileName,
                CreatedAt = dish.CreatedAt
            };
        }
    }
}
