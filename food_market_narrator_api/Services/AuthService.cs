using food_market_narrator_api.DTOs.Auth;
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

        public async Task<LoginResponseDto?> AuthenticateAsync(string username, string passwordHash)
        {
            var valid = await _userRepository.ValidateCredentialsAsync(username, passwordHash);
            if (!valid) return null;

            var user = await _userRepository.GetByUsernameAsync(username);
            if (user == null) return null;

            return new LoginResponseDto
            {
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username
                }
            };
        }
    }
}
