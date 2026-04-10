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
    [MaxLength(200)]
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    [MaxLength(500)]
    public string? UrlImage { get; set; }
    public bool IsActive { get; set; }
    /// <summary>Multipart file upload. If provided, overwrites UrlImage with the saved file path.</summary>
    public IFormFile? File { get; set; }
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
    /// <summary>
    /// Fallback image URL when no File is uploaded.
    /// When File is provided, this field is ignored.
    /// </summary>
    [MaxLength(500)]
    public string? UrlImage { get; set; }
    public bool IsActive { get; set; } = true;
    /// <summary>Multipart file upload. When provided, its saved path takes priority over UrlImage.</summary>
    public IFormFile? File { get; set; }
}
