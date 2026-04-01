using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/location-logs")]
[Authorize]
public class LocationLogsController : ControllerBase
{
    private readonly LocationLogService _locationLogService;

    public LocationLogsController(LocationLogService locationLogService)
    {
        _locationLogService = locationLogService;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> IngestBatch([FromBody] LocationLogBatchRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var insertedCount = await _locationLogService.WriteBatchAsync(request);
        return Ok(new
        {
            insertedCount
        });
    }
}
