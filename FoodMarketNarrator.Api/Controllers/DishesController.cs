using food_market_narrator_api.DTOs.Dish;
using food_market_narrator_api.Authorization;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        [AllowAnonymous]
        public async Task<IActionResult> GetByRestaurantId(
            string restaurantId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? languageCode = null)
        {
            page = page <= 0 ? 1 : page;
            pageSize = pageSize <= 0 ? 20 : pageSize;

            var data = await _dishService.GetByRestaurantIdAsync(restaurantId, page, pageSize, languageCode);
            return Ok(data);
        }

        [HttpPost("/Restaurant/{restaurantId}/dishes")]
        public async Task<IActionResult> Create(string restaurantId, [FromBody] CreateDishRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var created = await _dishService.CreateAsync(restaurantId, request);
            return Ok(created);
        }

        [HttpPut("/Dishes/{dishId:int}")]
        public async Task<IActionResult> Update(int dishId, [FromBody] UpdateDishRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var sellerUserId = await TryGetCurrentSellerUserIdAsync();

            DishResponse? updated;
            try
            {
                updated = await _dishService.UpdateAsync(dishId, request, sellerUserId);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (HttpRequestException ex)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = ex.Message });
            }

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

        private async Task<int?> TryGetCurrentSellerUserIdAsync()
        {
            var authResult = await HttpContext.AuthenticateAsync(AuthSchemes.Saler);
            var principal = authResult.Succeeded ? authResult.Principal : null;
            var userIdRaw = principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (int.TryParse(userIdRaw, out var userId))
            {
                return userId;
            }

            return null;
        }
    }
}
