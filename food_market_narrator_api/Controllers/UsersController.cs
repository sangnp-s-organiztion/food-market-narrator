using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly RestaurantService _restaurantService;

        public UsersController(RestaurantService restaurantService)
        {
            _restaurantService = restaurantService;
        }

        // GET api/users/{userId}/restaurants
        [HttpGet("{userId}/restaurants")]
        public async Task<IActionResult> GetUserRestaurants(int userId)
        {
            var data = await _restaurantService.GetRestaurantsByUserIdAsync(userId);
            return Ok(data);
        }
    }
}
