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
}
