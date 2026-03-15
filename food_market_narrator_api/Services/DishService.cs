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

        public async Task<List<DishResponseDto>> GetByRestaurantIdAsync(string restaurantId, int page, int pageSize)
        {
            var dishes = await _dishRepository.GetByRestaurantIdAsync(restaurantId, page, pageSize);
            return dishes.Select(Map).ToList();
        }

        public async Task<DishResponseDto?> CreateAsync(string restaurantId, CreateDishRequestDto request)
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

        public async Task<DishResponseDto?> UpdateAsync(int dishId, UpdateDishRequestDto request)
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

        private static DishResponseDto Map(DishModel dish)
        {
            return new DishResponseDto
            {
                DishId = dish.DishId,
                Name = dish.Name,
                Price = dish.Price,
                Description = dish.Description,
                RestaurantId = dish.RestaurantId,
                ImageId = dish.ImageId,
                CreatedAt = dish.CreatedAt
            };
        }
    }
}
