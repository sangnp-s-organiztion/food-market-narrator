using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Mongo;

public class UserSessionStartRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string DeviceId { get; set; } = string.Empty;

    public string? DeviceInfo { get; set; }
}
