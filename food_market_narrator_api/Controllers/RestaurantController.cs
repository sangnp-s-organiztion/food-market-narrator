using food_market_narrator_api.Services;
using food_market_narrator_api.DTOs.Restaurant;
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

        // PUT api/restaurant/{id}: Cập nhật toàn bộ nhà hàng
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] RestaurantRequestDto dto)
        {
            var updated = await _restaurantService.UpdateRestaurantAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // PATCH api/restaurant/{id}: Cập nhật một phần (giống như PUT hiện tại)
        [HttpPatch("{id}")]
        public async Task<IActionResult> Patch(string id, [FromBody] RestaurantRequestDto dto)
        {
            var updated = await _restaurantService.UpdateRestaurantAsync(id, dto);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // PATCH api/restaurant/{id}/activate : toggle active state
        public class ActivateRequest { public bool IsActive { get; set; } }

        [HttpPatch("{id}/activate")]
        public async Task<IActionResult> Activate(string id, [FromBody] ActivateRequest req)
        {
            var updated = await _restaurantService.SetActiveAsync(id, req.IsActive);
            if (updated == null) return NotFound();
            return Ok(updated);
        }
    }
}