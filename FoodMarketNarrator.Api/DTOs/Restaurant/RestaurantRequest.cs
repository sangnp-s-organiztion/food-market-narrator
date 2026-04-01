using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Restaurant
{
    public class RestaurantRequest
    {
        [Required]
        [MaxLength(255)]
        public string RestaurantId { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public bool IsActive { get; set; } = true;

        public List<RestaurantImageRequest> Images { get; set; } = new();
        public List<AudioRequest> Audios { get; set; } = new();
    }

    public class CreateRestaurantRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Required]
        public int UserId { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class RestaurantImageRequest
    {
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class AudioRequest
    {
        [Required]
        public int LanguageId { get; set; }

        [Required]
        [MaxLength(500)]
        public string AudioUrl { get; set; } = string.Empty;

        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }

    public class UpdateRestaurantRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string? Phone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        public decimal? Latitude { get; set; }
        public decimal? Longitude { get; set; }

        public TimeSpan? OpenTime { get; set; }
        public TimeSpan? CloseTime { get; set; }
    }

    public class UpdateRestaurantStatusRequest
    {
        public bool IsActive { get; set; }
    }

    public class ReorderImagesRequest
    {
        public List<ReorderImageItem> Items { get; set; } = new();
    }

    public class ReorderImageItem
    {
        public int ImageId { get; set; }
        public int SortOrder { get; set; }
    }

    public class SetPrimaryImageRequest
    {
        public bool IsPrimary { get; set; }
    }
}
