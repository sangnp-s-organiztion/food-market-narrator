using food_market_narrator_api.DTOs.Auth;

namespace food_market_narrator_api.DTOs.Auth
{
    public class UserDto
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
    }

    public class LoginResponseDto
    {
        public UserDto? User { get; set; }
        // future: token, expires
    }
}
