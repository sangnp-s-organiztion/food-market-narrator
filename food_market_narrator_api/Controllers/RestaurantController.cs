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
    // GET api/restaurant: Lấy danh sách tất cả restaurants
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _restaurantService.GetAllRestaurantsAsync();
            return Ok(data);
        }

    // GET api/restaurant/{id}: Lấy thông tin restaurant theo ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _restaurantService.GetRestaurantByIdAsync(id);
            return Ok(data);
        }
    }
}