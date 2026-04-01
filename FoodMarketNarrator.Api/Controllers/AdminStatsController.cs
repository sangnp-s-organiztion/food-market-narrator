using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/admin/stats")]
[Authorize]
public class AdminStatsController : ControllerBase
{
    private readonly RestaurantService _restaurantService;
    private readonly AudioService _audioService;
    private readonly UserService _userService;
    private readonly DishService _dishService;

    public AdminStatsController(
        RestaurantService restaurantService,
        AudioService audioService,
        UserService userService,
        DishService dishService)
    {
        _restaurantService = restaurantService;
        _audioService = audioService;
        _userService = userService;
        _dishService = dishService;
    }

    [HttpGet("restaurants/count")]
    public async Task<IActionResult> GetRestaurantCount()
    {
        int count = await _restaurantService.CountRestaurantsAsync();
        return Ok(new { count });
    }

    [HttpGet("audios/count")]
    public async Task<IActionResult> GetAudioCount()
    {
        int count = await _audioService.CountAudiosAsync();
        return Ok(new { count });
    }

    [HttpGet("users/count")]
    public async Task<IActionResult> GetUserCount()
    {
        int count = await _userService.CountUsersAsync();
        return Ok(new { count });
    }

    [HttpGet("dishes/count")]
    public async Task<IActionResult> GetDishCount()
    {
        int count = await _dishService.CountDishesAsync();
        return Ok(new { count });
    }
}
