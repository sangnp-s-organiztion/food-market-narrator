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
        private readonly UserService _userService;
        private readonly AuditLogService _auditLogService;

        public AuthController(
            AuthService authService,
            UserService userService,
            AuditLogService auditLogService)
        {
            _authService = authService;
            _userService = userService;
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

        [HttpPost("forgot-password/send-otp")]
        public async Task<IActionResult> SendForgotPasswordOtp([FromBody] ForgotPasswordSendOtpRequest request)
        {
            var result = await _authService.SendForgotPasswordOtpAsync(request.Username, request.Email);
            return result.Status switch
            {
                ForgotPasswordSendOtpStatus.Success => Ok(new ForgotPasswordSendOtpResponse
                {
                    Message = "Da gui OTP qua gmail.",
                    ExpiresInSeconds = result.ExpiresInSeconds
                }),
                ForgotPasswordSendOtpStatus.InvalidInput => BadRequest(new { message = "Username va gmail la bat buoc." }),
                ForgotPasswordSendOtpStatus.UsernameNotFound => NotFound(new { message = "User name khong ton tai." }),
                ForgotPasswordSendOtpStatus.EmailMismatch => BadRequest(new { message = "Gmail bi sai." }),
                ForgotPasswordSendOtpStatus.NotFoundBoth => NotFound(new { message = "Thong tin khong ton tai." }),
                ForgotPasswordSendOtpStatus.EmailNotFound => BadRequest(new { message = "Gmail khong ton tai." }),
                ForgotPasswordSendOtpStatus.EmailDeliveryFailed => BadRequest(new { message = "Khong the gui OTP. Vui long kiem tra cau hinh Gmail SMTP." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Khong the gui OTP." })
            };
        }

        [HttpPost("forgot-password/reset")]
        public async Task<IActionResult> ResetForgotPassword([FromBody] ForgotPasswordResetRequest request)
        {
            var result = await _authService.ResetForgotPasswordAsync(
                request.Username,
                request.Email,
                request.Otp,
                request.NewPassword);

            return result.Status switch
            {
                ForgotPasswordResetStatus.Success => Ok(new { message = "Dat lai mat khau thanh cong." }),
                ForgotPasswordResetStatus.InvalidInput => BadRequest(new { message = "Vui long nhap day du thong tin." }),
                ForgotPasswordResetStatus.InvalidNewPassword => BadRequest(new { message = "Mat khau moi phai co it nhat 6 ky tu." }),
                ForgotPasswordResetStatus.InvalidOtp => BadRequest(new { message = "OTP khong dung." }),
                ForgotPasswordResetStatus.OtpExpired => BadRequest(new { message = "Het han OTP, vui long gui lai." }),
                ForgotPasswordResetStatus.UsernameNotFound => NotFound(new { message = "User name khong ton tai." }),
                ForgotPasswordResetStatus.EmailMismatch => BadRequest(new { message = "Gmail bi sai." }),
                ForgotPasswordResetStatus.NotFoundBoth => NotFound(new { message = "Thong tin khong ton tai." }),
                ForgotPasswordResetStatus.EmailNotFound => BadRequest(new { message = "Gmail khong ton tai." }),
                _ => StatusCode(StatusCodes.Status500InternalServerError, new { message = "Khong the dat lai mat khau." })
            };
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
            var currentUserIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdRaw, out var currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            MeResponse? me = await _authService.GetMeByUserIdAsync(currentUserId);
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
            var currentUserIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdRaw, out var currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            MeResponse? me = await _authService.GetMeByUserIdAsync(currentUserId);
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

        [HttpPatch("password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            var currentUserIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdRaw, out var currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            bool updated;
            try
            {
                updated = await _userService.ChangePasswordAsync(
                    currentUserId,
                    request.OldPassword,
                    request.NewPassword);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (!updated)
            {
                return NotFound(new { message = "User not found." });
            }

            return Ok(new { message = "Password updated." });
        }

        [HttpPatch("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            var currentUserIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(currentUserIdRaw, out var currentUserId))
            {
                return Unauthorized(new { message = "Unauthorized." });
            }

            DTOs.User.UserResponse? updated;
            try
            {
                updated = await _userService.UpdateProfileAsync(
                    currentUserId,
                    request.Username,
                    request.Phone,
                    request.Email);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }

            if (updated == null)
            {
                return NotFound(new { message = "User not found." });
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, updated.UserId.ToString()),
                new Claim(ClaimTypes.Name, updated.Username),
                new Claim(ClaimTypes.Role, updated.Role)
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

            return Ok(new
            {
                updated.UserId,
                updated.Username,
                updated.Role,
                updated.Phone,
                updated.Email,
                updated.IsActive,
                updated.CreatedAt
            });
        }
    }
}
