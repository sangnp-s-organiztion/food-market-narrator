using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Models;
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
    private readonly AuditLogService _auditLogService;

    public LocationLogsController(LocationLogService locationLogService, AuditLogService auditLogService)
    {
        _locationLogService = locationLogService;
        _auditLogService = auditLogService;
    }

    [HttpPost("batch")]
    public async Task<IActionResult> IngestBatch([FromBody] LocationLogBatchRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var insertedCount = await _locationLogService.WriteBatchAsync(request);

        var firstSessionId = request.Items.FirstOrDefault()?.SessionId;
        var normalizedSessionId = string.IsNullOrWhiteSpace(firstSessionId) ? null : firstSessionId.Trim();
        var hasLoggedThisSession = normalizedSessionId != null
            && await _auditLogService.ExistsAsync("MOBILE_SYNC", normalizedSessionId);

        if (!hasLoggedThisSession)
        {
            await _auditLogService.WriteLogAsync(new AuditLog
            {
                UserId = 0,
                Username = "mobile",
                Action = "MOBILE_SYNC",
                TargetType = "LocationLogs",
                TargetId = normalizedSessionId,
                Details = null,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });
        }

        return Ok(new
        {
            insertedCount
        });
    }
}
