using food_market_narrator_api.DTOs.Dish;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class DishService
    {
        private readonly DishRepository _dishRepository;

        public DishService(DishRepository dishRepository)
        {
            _dishRepository = dishRepository;
        }

        public async Task<List<DishResponse>> GetByRestaurantIdAsync(string restaurantId, int page, int pageSize)
        {
            var dishes = await _dishRepository.GetByRestaurantIdAsync(restaurantId, page, pageSize);
            return dishes.Select(Map).ToList();
        }

        public async Task<DishResponse?> CreateAsync(string restaurantId, CreateDishRequest request)
        {
            var dish = new DishModel
            {
                Name = request.Name.Trim(),
                Price = request.Price,
                Description = request.Description,
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
            existing.Description = request.Description;
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
                Description = dish.Description,
                RestaurantId = dish.RestaurantId,
                ImageId = dish.ImageId,
                ImageFileName = imageFileName,
                CreatedAt = dish.CreatedAt
            };
        }
    }
}
