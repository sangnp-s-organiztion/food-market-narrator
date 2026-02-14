using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RestaurantController : ControllerBase
    {
        private readonly RestaurantService _restaurantService;

        public RestaurantController(RestaurantService restaurantService)
        {
            _restaurantService = restaurantService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _restaurantService.GetAllRestaurantsAsync();
            return Ok(data);
        }
    }
}