using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("public")]
    public class PublicDataController : ControllerBase
    {
        private readonly DishService _dishService;
        private readonly RestaurantService _restaurantService;
        private readonly AudioService _audioService;

        public PublicDataController(DishService dishService, RestaurantService restaurantService, AudioService audioService)
        {
            _dishService = dishService;
            _restaurantService = restaurantService;
            _audioService = audioService;
        }

        [HttpGet("/public/Restaurant/{restaurantId}/dishes")]
        public async Task<IActionResult> GetDishes(string restaurantId, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 100 : pageSize;
            var data = await _dishService.GetByRestaurantIdAsync(restaurantId, page, pageSize);
            return Ok(data);
        }

        [HttpGet("/public/Restaurant/{restaurantId}/images")]
        public async Task<IActionResult> GetImages(string restaurantId)
        {
            var data = await _restaurantService.GetImagesByRestaurantIdAsync(restaurantId);
            return Ok(data);
        }

        [HttpGet("/public/Restaurant/{restaurantId}/audios")]
        public async Task<IActionResult> GetAudios(string restaurantId)
        {
            var data = await _audioService.GetByRestaurantIdAsync(restaurantId);
            return Ok(data);
        }
    }
}
