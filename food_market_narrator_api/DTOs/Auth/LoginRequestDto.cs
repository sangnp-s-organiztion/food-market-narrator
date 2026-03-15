namespace food_market_narrator_api.DTOs.Auth
{
    public class LoginRequestDto
    {
        public string Username { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
    }
}
