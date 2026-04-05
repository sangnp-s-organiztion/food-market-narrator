using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Models;
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
    private readonly AuditLogService _auditLogService;

    public AudioLogsController(AudioLogService audioLogService, AuditLogService auditLogService)
    {
        _audioLogService = audioLogService;
        _auditLogService = auditLogService;
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
            await _auditLogService.WriteLogAsync(new AuditLog
            {
                UserId = 0,
                Username = "mobile",
                Action = "MOBILE_PLAY",
                TargetType = "AudioLogs",
                TargetId = request.SessionId,
                Details = $"status=session-not-found, restaurantId={request.RestaurantId}, audioId={request.AudioId}",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            return NotFound(new
            {
                message = "Session not found"
            });
        }

        await _auditLogService.WriteLogAsync(new AuditLog
        {
            UserId = 0,
            Username = "mobile",
            Action = "MOBILE_PLAY",
            TargetType = "AudioLogs",
            TargetId = request.SessionId,
            Details = $"restaurantId={request.RestaurantId}, audioId={request.AudioId}, duration={request.Duration}",
            IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
            CreatedAt = DateTime.UtcNow
        });

        return Ok(new
        {
            inserted = true
        });
    }
}
