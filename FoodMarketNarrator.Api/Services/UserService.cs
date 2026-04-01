using food_market_narrator_api.DTOs.User;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services;

public class UserService
{
    private readonly UserRepository _userRepository;

    public UserService(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<UserResponse>> GetAllUsersAsync()
    {
        var users = await _userRepository.GetAllAsync();
        return users.Select(MapUser).ToList();
    }

    public async Task<int> CountUsersAsync()
    {
        return await _userRepository.CountAsync();
    }

    public async Task<UserResponse?> GetUserByIdAsync(int userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        return user == null ? null : MapUser(user);
    }

    public async Task<UserResponse?> CreateUserAsync(CreateUserRequest request)
    {
        var existing = await _userRepository.GetByUsernameAsync(request.Username);
        if (existing != null)
        {
            return null; // username already taken
        }

        var normalizedRole = UserRoleParser.NormalizeOrThrow(request.Role);

        var user = new UserModel
        {
            Username = request.Username.Trim(),
            Password = request.Password, // store as-is to match existing DB schema
            Role = normalizedRole,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userRepository.CreateAsync(user);
        return MapUser(created);
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, string role)
    {
        var normalizedRole = UserRoleParser.NormalizeOrThrow(role);
        return await _userRepository.UpdateRoleAsync(userId, normalizedRole);
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
    {
        return await _userRepository.UpdateStatusAsync(userId, isActive);
    }

    private static UserResponse MapUser(UserModel u)
    {
        var normalizedRole = UserRoleParser.TryParse(u.Role, out var parsedRole)
            ? parsedRole.ToString()
            : (u.Role ?? string.Empty).Trim().ToLowerInvariant();

        return new UserResponse
        {
            UserId = u.UserId,
            Username = u.Username,
            Role = normalizedRole,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
