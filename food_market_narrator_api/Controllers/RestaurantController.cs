using food_market_narrator_api.Services;
using food_market_narrator_api.DTOs.Restaurant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    [Authorize]
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
            if (data == null)
            {
                return NotFound(new { message = "Restaurant not found." });
            }

            return Ok(data);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRestaurantRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var updated = await _restaurantService.UpdateRestaurantAsync(id, request);
            if (updated == null)
            {
                return NotFound(new { message = "Restaurant not found." });
            }

            return Ok(updated);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateRestaurantStatusRequestDto request)
        {
            bool updated = await _restaurantService.UpdateRestaurantStatusAsync(id, request.IsActive);
            if (!updated)
            {
                return NotFound(new { message = "Restaurant not found." });
            }

            return Ok(new { message = "Restaurant status updated." });
        }
    }
}