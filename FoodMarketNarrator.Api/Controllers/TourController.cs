using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("[controller]")]
[Authorize]
public class TourController : ControllerBase
{
    private readonly TourService _tourService;

    public TourController(TourService tourService)
    {
        _tourService = tourService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double radiusMeters = 30)
    {
        if (radiusMeters <= 0)
        {
            return BadRequest(new { message = "radiusMeters must be greater than 0." });
        }

        var data = await _tourService.GetAllToursAsync(latitude, longitude, radiusMeters);
        return Ok(data);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        [FromQuery] double? latitude = null,
        [FromQuery] double? longitude = null,
        [FromQuery] double radiusMeters = 30)
    {
        if (radiusMeters <= 0)
        {
            return BadRequest(new { message = "radiusMeters must be greater than 0." });
        }

        var data = await _tourService.GetTourByIdAsync(id, latitude, longitude, radiusMeters);
        if (data == null)
        {
            return NotFound(new { message = "Tour not found." });
        }

        return Ok(data);
    }
}
