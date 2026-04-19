using food_market_narrator_api.DTOs.Dish;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class DishService
    {
        private readonly DishRepository _dishRepository;
        private readonly LanguageRepository _languageRepository;
        private readonly RestaurantRepository _restaurantRepository;
        private readonly TranslationService _translationService;

        public DishService(
            DishRepository dishRepository,
            LanguageRepository languageRepository,
            RestaurantRepository restaurantRepository,
            TranslationService translationService)
        {
            _dishRepository = dishRepository;
            _languageRepository = languageRepository;
            _restaurantRepository = restaurantRepository;
            _translationService = translationService;
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

            var ownerUserId = await TryGetRestaurantOwnerUserIdAsync(restaurantId);
            if (ownerUserId.HasValue)
            {
                await _translationService.SyncDishNameTranslationsAsync(
                    ownerUserId.Value,
                    restaurantId,
                    created.DishId,
                    created.Name,
                    requestIdPrefix: $"dish-create-{created.DishId}");
            }

            return Map(created);
        }

        public async Task<DishResponse?> UpdateAsync(
            int dishId,
            UpdateDishRequest request,
            int? sellerUserId = null)
        {
            var existing = await _dishRepository.GetByIdAsync(dishId);
            if (existing == null)
            {
                return null;
            }

            var normalizedName = request.Name.Trim();
            var isDishNameChanged = !string.Equals(
                NormalizeForComparison(existing.Name),
                NormalizeForComparison(normalizedName),
                StringComparison.Ordinal);

            existing.Name = normalizedName;
            existing.Price = request.Price;
            existing.ImageId = request.ImageId;

            bool updated = await _dishRepository.UpdateAsync(existing);
            if (!updated)
            {
                return null;
            }

            if (sellerUserId.HasValue && isDishNameChanged)
            {
                await _translationService.SyncDishNameTranslationsAsync(
                    sellerUserId.Value,
                    existing.RestaurantId,
                    existing.DishId,
                    existing.Name,
                    requestIdPrefix: $"dish-update-{existing.DishId}");
            }

            return Map(existing);
        }

        private static string NormalizeForComparison(string? value)
        {
            return (value ?? string.Empty).Trim();
        }

        private async Task<int?> TryGetRestaurantOwnerUserIdAsync(string restaurantId)
        {
            var restaurant = await _restaurantRepository.GetByIdAsync(restaurantId);
            return restaurant?.UserId;
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
