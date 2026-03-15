using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Dish
{
    public class DishResponseDto
    {
        public int DishId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal? Price { get; set; }
        public string? Description { get; set; }
        public string RestaurantId { get; set; } = string.Empty;
        public int? ImageId { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    public class CreateDishRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? ImageId { get; set; }
    }

    public class UpdateDishRequestDto
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? ImageId { get; set; }
    }
}
