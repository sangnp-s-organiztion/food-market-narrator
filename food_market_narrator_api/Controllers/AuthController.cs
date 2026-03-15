using food_market_narrator_api.DTOs.Auth;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;

        public AuthController(AuthService authService)
        {
            _authService = authService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.PasswordHash))
                return BadRequest(new { error = "username and password_hash are required" });

            var result = await _authService.AuthenticateAsync(request.Username, request.PasswordHash);
            if (result == null) return Unauthorized(new { error = "Invalid credentials" });

            return Ok(result);
        }
    }
}
