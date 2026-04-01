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

        var user = new UserModel
        {
            Username = request.Username.Trim(),
            Password = request.Password, // store as-is to match existing DB schema
            Role = request.Role,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _userRepository.CreateAsync(user);
        return MapUser(created);
    }

    public async Task<bool> UpdateUserRoleAsync(int userId, string role)
    {
        return await _userRepository.UpdateRoleAsync(userId, role);
    }

    public async Task<bool> UpdateUserStatusAsync(int userId, bool isActive)
    {
        return await _userRepository.UpdateStatusAsync(userId, isActive);
    }

    private static UserResponse MapUser(UserModel u)
    {
        return new UserResponse
        {
            UserId = u.UserId,
            Username = u.Username,
            Role = u.Role,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
