using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Tour;

public class AddTourRestaurantRequest
{
    [Required]
    [MaxLength(100)]
    public string RestaurantId { get; set; } = string.Empty;
}
