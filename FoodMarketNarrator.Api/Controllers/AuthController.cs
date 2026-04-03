using System.Security.Claims;
using food_market_narrator_api.DTOs.Auth;
using food_market_narrator_api.Models;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthController : ControllerBase
    {
        private const string AdminRole = "admin";
        private readonly AuthService _authService;
        private readonly AuditLogService _auditLogService;

        public AuthController(AuthService authService, AuditLogService auditLogService)
        {
            _authService = authService;
            _auditLogService = auditLogService;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            UserModel? user = await _authService.ValidateCredentialsAsync(request.Username.Trim(), request.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            await _auditLogService.WriteLogAsync(new AuditLog
            {
                UserId = user.UserId,
                Username = user.Username,
                Action = "LOGIN",
                TargetType = "User",
                TargetId = user.UserId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            });

            return Ok(new LoginResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive
            });
        }

        [HttpPost("admin/login")]
        public async Task<IActionResult> AdminLogin([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(new { message = "Username and password are required." });
            }

            UserModel? user = await _authService.ValidateCredentialsAsync(request.Username.Trim(), request.Password);
            if (user == null)
            {
                return Unauthorized(new { message = "Invalid credentials." });
            }

            if (!string.Equals(user.Role, AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });

            await _auditLogService.WriteLogAsync(new AuditLog
            {
                UserId = user.UserId,
                Username = user.Username,
                Action = "LOGIN",
                TargetType = "User",
                TargetId = user.UserId.ToString(),
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow,
                Details = "Admin portal login"
            });

            return Ok(new LoginResponse
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive
            });
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var username = User.Identity?.Name ?? "unknown";

            if (int.TryParse(userIdClaim, out var uid))
            {
                await _auditLogService.WriteLogAsync(new AuditLog
                {
                    UserId = uid,
                    Username = username,
                    Action = "LOGOUT",
                    TargetType = "User",
                    TargetId = uid.ToString(),
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    CreatedAt = DateTime.UtcNow
                });
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> Me()
        {
            string? username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            MeResponse? me = await _authService.GetMeAsync(username);
            if (me == null)
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            return Ok(me);
        }

        [HttpGet("admin/me")]
        [Authorize]
        public async Task<IActionResult> AdminMe()
        {
            string? username = User.Identity?.Name;
            if (string.IsNullOrWhiteSpace(username))
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            MeResponse? me = await _authService.GetMeAsync(username);
            if (me == null)
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            if (!string.Equals(me.Role, AdminRole, StringComparison.OrdinalIgnoreCase))
            {
                return Forbid();
            }

            return Ok(me);
        }
    }
}
