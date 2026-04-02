using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/audio-logs")]
[Authorize]
public class AudioLogsController : ControllerBase
{
    private readonly AudioLogService _audioLogService;

    public AudioLogsController(AudioLogService audioLogService)
    {
        _audioLogService = audioLogService;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] AudioLogCreateRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var inserted = await _audioLogService.WriteAsync(request);
        if (!inserted)
        {
            return NotFound(new
            {
                message = "Session not found"
            });
        }

        return Ok(new
        {
            inserted = true
        });
    }
}
