using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class RestaurantService
    {
        private readonly RestaurantRepository _restaurantRepository;
        public RestaurantService(RestaurantRepository repository)
        {
            _restaurantRepository = repository;
        }
        public async Task<List<RestaurantModel>> GetAllRestaurantsAsync()
        {
            return await _restaurantRepository.GetAllAsync();
        }
    }
}
