using food_market_narrator_api.DTOs.User;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;

    public UsersController(UserService userService)
    {
        _userService = userService;
    }

    // GET api/users
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    // GET api/users/{id}
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found." });
        }
        return Ok(user);
    }

    // POST api/users
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            return BadRequest(new { message = "Username is required." });
        }

        UserResponse? created;
        try
        {
            created = await _userService.CreateUserAsync(request);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (created == null)
        {
            return Conflict(new { message = "Username already exists." });
        }

        return CreatedAtAction(nameof(GetById), new { id = created.UserId }, created);
    }

    // PATCH api/users/{id}/role
    [HttpPatch("{id:int}/role")]
    public async Task<IActionResult> UpdateRole(int id, [FromBody] UpdateUserRoleRequest request)
    {
        bool updated;
        try
        {
            updated = await _userService.UpdateUserRoleAsync(id, request.Role);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }

        if (!updated)
        {
            return NotFound(new { message = "User not found." });
        }
        return Ok(new { message = "User role updated." });
    }

    // PATCH api/users/{id}/status
    [HttpPatch("{id:int}/status")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        if (!request.IsActive)
        {
            var currentUserIdRaw = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(currentUserIdRaw, out var currentUserId) && currentUserId == id)
            {
                return BadRequest(new { message = "Không thể khóa tài khoản admin đang đăng nhập." });
            }
        }

        bool updated = await _userService.UpdateUserStatusAsync(id, request.IsActive);
        if (!updated)
        {
            return NotFound(new { message = "User not found." });
        }
        return Ok(new { message = "User status updated." });
    }
}
