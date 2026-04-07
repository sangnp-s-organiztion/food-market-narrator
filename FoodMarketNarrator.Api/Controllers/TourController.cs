using food_market_narrator_api.Services;
using food_market_narrator_api.DTOs.Tour;
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

    [HttpPost("{id:int}/restaurants")]
    public async Task<IActionResult> AddRestaurantToTour(int id, [FromBody] AddTourRestaurantRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _tourService.AddRestaurantToTourAsync(id, request.RestaurantId);

        return result.Status switch
        {
            AddTourRestaurantStatus.Success => Ok(new { message = "Restaurant added to tour." }),
            AddTourRestaurantStatus.NotFound => NotFound(new { message = result.Message }),
            AddTourRestaurantStatus.Conflict => Conflict(new { message = result.Message }),
            AddTourRestaurantStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = "Unable to add restaurant to tour." })
        };
    }

    [HttpPut("{id:int}/stops/order")]
    public async Task<IActionResult> ReorderStops(int id, [FromBody] ReorderTourStopsRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _tourService.ReorderTourStopsAsync(id, request.RestaurantIds);
        return result.Status switch
        {
            ReorderTourStopsStatus.Success => Ok(new { message = "Tour stop order updated." }),
            ReorderTourStopsStatus.NotFound => NotFound(new { message = result.Message }),
            ReorderTourStopsStatus.Invalid => BadRequest(new { message = result.Message }),
            _ => BadRequest(new { message = "Unable to reorder stops." })
        };
    }
}
