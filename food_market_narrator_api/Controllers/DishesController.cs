using food_market_narrator_api.DTOs.Dish;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Authorize]
    public class DishesController : ControllerBase
    {
        private readonly DishService _dishService;

        public DishesController(DishService dishService)
        {
            _dishService = dishService;
        }

        [HttpGet("/Restaurant/{restaurantId}/dishes")]
        public async Task<IActionResult> GetByRestaurantId(
            string restaurantId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var data = await _dishService.GetByRestaurantIdAsync(restaurantId, page, pageSize);
            return Ok(data);
        }

        [HttpPost("/Restaurant/{restaurantId}/dishes")]
        public async Task<IActionResult> Create(string restaurantId, [FromBody] CreateDishRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var created = await _dishService.CreateAsync(restaurantId, request);
            return Ok(created);
        }

        [HttpPut("/Dishes/{dishId:int}")]
        public async Task<IActionResult> Update(int dishId, [FromBody] UpdateDishRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var updated = await _dishService.UpdateAsync(dishId, request);
            if (updated == null)
            {
                return NotFound(new { message = "Dish not found." });
            }

            return Ok(updated);
        }

        [HttpDelete("/Dishes/{dishId:int}")]
        public async Task<IActionResult> Delete(int dishId)
        {
            bool deleted = await _dishService.DeleteAsync(dishId);
            if (!deleted)
            {
                return NotFound(new { message = "Dish not found." });
            }

            return Ok(new { message = "Dish deleted successfully." });
        }
    }
}
