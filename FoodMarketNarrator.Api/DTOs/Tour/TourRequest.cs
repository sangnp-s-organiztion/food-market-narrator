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
    public bool IsActive { get; set; }
    public bool IsFeatured { get; set; }
}

public class CreateTourRequest
{
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsFeatured { get; set; }
    public int SortPriority { get; set; }
}
