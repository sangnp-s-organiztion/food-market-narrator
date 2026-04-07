using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models;

[Table("Tour")]
public class TourModel
{
    [Key]
    [Column("tour_id")]
    public int TourId { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Column("short_description")]
    [MaxLength(500)]
    public string? ShortDescription { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("estimated_duration_minutes")]
    public int? EstimatedDurationMinutes { get; set; }

    [Column("image_id")]
    public int? ImageId { get; set; }

    [Column("is_active")]
    public bool IsActive { get; set; }

    [Column("is_featured")]
    public bool IsFeatured { get; set; }

    [Column("sort_priority")]
    public int SortPriority { get; set; }

    [Column("created_by")]
    public int? CreatedBy { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("updated_by")]
    public int? UpdatedBy { get; set; }

    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; }

    [ForeignKey(nameof(ImageId))]
    public RestaurantImageModel? Image { get; set; }

    public ICollection<TourRestaurantModel> TourRestaurants { get; set; } = new List<TourRestaurantModel>();
}

[Table("Tour_Restaurant")]
public class TourRestaurantModel
{
    [Column("tour_id")]
    public int TourId { get; set; }

    [Column("restaurant_id")]
    [MaxLength(100)]
    public string RestaurantId { get; set; } = string.Empty;

    [Column("stop_order")]
    public int StopOrder { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [ForeignKey(nameof(TourId))]
    public TourModel Tour { get; set; } = null!;

    [ForeignKey(nameof(RestaurantId))]
    public RestaurantModel Restaurant { get; set; } = null!;
}
