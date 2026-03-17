using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Restaurant
{
    public class UpdateRestaurantRequestDto
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

    public class UpdateRestaurantStatusRequestDto
    {
        public bool IsActive { get; set; }
    }

    public class ReorderImagesRequestDto
    {
        public List<ReorderImageItemDto> Items { get; set; } = new();
    }

    public class ReorderImageItemDto
    {
        public int ImageId { get; set; }
        public int SortOrder { get; set; }
    }

    public class SetPrimaryImageRequestDto
    {
        public bool IsPrimary { get; set; }
    }
}
