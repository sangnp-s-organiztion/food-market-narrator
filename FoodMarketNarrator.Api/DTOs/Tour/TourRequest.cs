using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Tour;

public class AddTourRestaurantRequest
{
    [Required]
    [MaxLength(100)]
    public string RestaurantId { get; set; } = string.Empty;
}

public class ReorderTourStopsRequest
{
    [Required]
    public List<string> RestaurantIds { get; set; } = new();
}

public class UpdateTourRequest
{
    public int? EstimatedDurationMinutes { get; set; }
    public int SortPriority { get; set; }
    public bool IsFeatured { get; set; }
}
