using food_market_narrator_api.DTOs.User;
using food_market_narrator_api.Helpers;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;
using System.Text.RegularExpressions;

namespace food_market_narrator_api.Services;

public class UserService
{
    private const string DefaultPassword = "123456";
    private static readonly Regex PhoneRegex = new(@"^0\d{9,10}$", RegexOptions.Compiled);
    private static readonly Regex EmailRegex = new(
        @"^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}$",
        RegexOptions.Compiled);
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
        var normalizedPhone = (request.Phone ?? string.Empty).Trim();
        var normalizedEmail = (request.Email ?? string.Empty).Trim();

        if (!PhoneRegex.IsMatch(normalizedPhone))
        {
            throw new ArgumentException("Số điện thoại không hợp lệ. Định dạng hợp lệ: bắt đầu bằng 0, gồm 10-11 chữ số.");
        }

        if (!EmailRegex.IsMatch(normalizedEmail))
        {
            throw new ArgumentException("Email không hợp lệ.");
        }

        var existing = await _userRepository.GetByUsernameAsync(request.Username);
        if (existing != null)
        {
            return null; // username already taken
        }

        var normalizedRole = UserRoleParser.NormalizeOrThrow(request.Role);

        var password = string.IsNullOrWhiteSpace(request.Password)
            ? DefaultPassword
            : request.Password;

        var user = new UserModel
        {
            Username = request.Username.Trim(),
            Password = PasswordHasher.Hash(password),
            Phone = normalizedPhone,
            Email = normalizedEmail,
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
            Phone = u.Phone,
            Email = u.Email,
            Role = normalizedRole,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
