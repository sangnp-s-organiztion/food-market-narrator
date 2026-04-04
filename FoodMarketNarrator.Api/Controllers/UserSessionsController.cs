using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/user-sessions")]
[Authorize]
public class UserSessionsController : ControllerBase
{
    private readonly UserSessionService _userSessionService;

    public UserSessionsController(UserSessionService userSessionService)
    {
        _userSessionService = userSessionService;
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartSession([FromBody] UserSessionStartRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        await _userSessionService.StartSessionAsync(request);

        return Ok(new
        {
            started = true,
            sessionId = request.SessionId.Trim()
        });
    }

    [HttpGet("{sessionId}/qr-access")]
    public async Task<IActionResult> GetQrAccessStatus([FromRoute] string sessionId)
    {
        var normalizedSessionId = (sessionId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedSessionId))
        {
            return BadRequest(new
            {
                message = "SessionId is required"
            });
        }

        var status = await _userSessionService.GetQrAccessStatusAsync(normalizedSessionId);
        if (!status.Exists)
        {
            return NotFound(new
            {
                message = "Session not found"
            });
        }

        return Ok(new
        {
            allowed = status.Allowed,
            expiresAtUtc = status.ExpiresAtUtc,
            reason = status.Reason
        });
    }
}
