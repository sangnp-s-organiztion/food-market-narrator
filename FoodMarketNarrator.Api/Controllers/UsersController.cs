using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly RestaurantService _restaurantService;

        public UsersController(RestaurantService restaurantService)
        {
            _restaurantService = restaurantService;
        }

        [HttpGet("{userId:int}/restaurants")]
        public async Task<IActionResult> GetRestaurantsByUserId(int userId)
        {
            var data = await _restaurantService.GetRestaurantsByUserIdAsync(userId);
            return Ok(data);
        }
    }
}
