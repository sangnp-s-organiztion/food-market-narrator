using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Restaurant
{
    public class RestaurantRequestDto
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

        public List<RestaurantImageRequestDto> Images { get; set; } = new();
        public List<AudioRequestDto> Audios { get; set; } = new();
    }

    public class RestaurantImageRequestDto
    {
        [Required]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        public bool IsPrimary { get; set; }
        public int SortOrder { get; set; }
    }

    public class AudioRequestDto
    {
        [Required]
        public int LanguageId { get; set; }

        [Required]
        [MaxLength(500)]
        public string AudioUrl { get; set; } = string.Empty;

        public int Version { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
