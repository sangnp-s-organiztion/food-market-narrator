using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("[controller]")]
public class MongoController : ControllerBase
{
    private readonly MongoHealthService _mongoHealthService;

    public MongoController(MongoHealthService mongoHealthService)
    {
        _mongoHealthService = mongoHealthService;
    }

    [HttpGet("test-connect")]
    public async Task<IActionResult> TestMongoConnection()
    {
        var result = await _mongoHealthService.TestConnectionAsync();

        return result.Success ? Ok(result) : StatusCode(StatusCodes.Status503ServiceUnavailable, result);
    }
}
