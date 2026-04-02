using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Mongo;

public class AudioLogCreateRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    [Required]
    public string RestaurantId { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int AudioId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    [Range(0, int.MaxValue)]
    public int Duration { get; set; }
}
