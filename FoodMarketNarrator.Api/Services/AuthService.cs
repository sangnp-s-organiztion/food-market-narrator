using food_market_narrator_api.DTOs.Auth;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services
{
    public class AuthService
    {
        private readonly UserRepository _userRepository;

        public AuthService(UserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<UserModel?> ValidateCredentialsAsync(string username, string password)
        {
            bool isValid = await _userRepository.ValidateCredentialsAsync(username, password);
            if (!isValid)
            {
                return null;
            }

            return await _userRepository.GetByUsernameAsync(username);
        }

        public async Task<LoginResponseDto?> LoginAsync(string username, string password)
        {
            UserModel? user = await ValidateCredentialsAsync(username, password);
            if (user == null)
            {
                return null;
            }

            return new LoginResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role,
                IsActive = user.IsActive
            };
        }

        public async Task<MeResponseDto?> GetMeAsync(string username)
        {
            UserModel? user = await _userRepository.GetByUsernameAsync(username);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return new MeResponseDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Role = user.Role
            };
        }
    }
}
