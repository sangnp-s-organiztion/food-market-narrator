using food_market_narrator_api.Services;
using food_market_narrator_api.DTOs.Restaurant;
using food_market_narrator_api.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateRestaurantRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var created = await _restaurantService.CreateRestaurantAsync(request);
            return Ok(created);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateRestaurantRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var sellerUserId = await TryGetCurrentSellerUserIdAsync();

            RestaurantResponse? updated;
            try
            {
                updated = await _restaurantService.UpdateRestaurantAsync(id, request, sellerUserId);
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
                return NotFound(new { message = "Restaurant not found." });
            }

            return Ok(updated);
        }

        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(string id, [FromBody] UpdateRestaurantStatusRequest request)
        {
            bool updated = await _restaurantService.UpdateRestaurantStatusAsync(id, request.IsActive);
            if (!updated)
            {
                return NotFound(new { message = "Restaurant not found." });
            }

            return Ok(new { message = "Restaurant status updated." });
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