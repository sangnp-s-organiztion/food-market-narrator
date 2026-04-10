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

    public async Task<bool> ChangePasswordAsync(int userId, string oldPassword, string newPassword)
    {
        if (string.IsNullOrWhiteSpace(oldPassword))
        {
            throw new ArgumentException("Mật khẩu cũ là bắt buộc.");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("Mật khẩu mới là bắt buộc.");
        }

        if (newPassword.Trim().Length < 6)
        {
            throw new ArgumentException("Mật khẩu mới phải có ít nhất 6 ký tự.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null)
        {
            return false;
        }

        var isOldPasswordValid = PasswordHasher.IsHashed(user.Password)
            ? PasswordHasher.Verify(oldPassword, user.Password)
            : string.Equals(user.Password, oldPassword, StringComparison.Ordinal);

        if (!isOldPasswordValid)
        {
            throw new ArgumentException("Mật khẩu cũ không đúng.");
        }

        if (string.Equals(oldPassword, newPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("Mật khẩu mới không được trùng mật khẩu cũ.");
        }

        return await _userRepository.UpdatePasswordAsync(userId, PasswordHasher.Hash(newPassword));
    }

    public async Task<UserResponse?> UpdateProfileAsync(int userId, string username, string phone, string email)
    {
        var normalizedUsername = (username ?? string.Empty).Trim();
        var normalizedPhone = (phone ?? string.Empty).Trim();
        var normalizedEmail = (email ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(normalizedUsername))
        {
            throw new ArgumentException("Tên đăng nhập là bắt buộc.");
        }

        if (!PhoneRegex.IsMatch(normalizedPhone))
        {
            throw new ArgumentException("Số điện thoại không hợp lệ. Định dạng hợp lệ: bắt đầu bằng 0, gồm 10-11 chữ số.");
        }

        if (!EmailRegex.IsMatch(normalizedEmail))
        {
            throw new ArgumentException("Email không hợp lệ.");
        }

        var duplicate = await _userRepository.GetByUsernameAsync(normalizedUsername);
        if (duplicate != null && duplicate.UserId != userId)
        {
            throw new ArgumentException("Tên đăng nhập đã tồn tại.");
        }

        var updated = await _userRepository.UpdateProfileAsync(
            userId,
            normalizedUsername,
            normalizedPhone,
            normalizedEmail);

        if (!updated)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(userId);
        return user == null ? null : MapUser(user);
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
            FullName = u.FullName,
            Role = normalizedRole,
            IsActive = u.IsActive,
            CreatedAt = u.CreatedAt
        };
    }
}
