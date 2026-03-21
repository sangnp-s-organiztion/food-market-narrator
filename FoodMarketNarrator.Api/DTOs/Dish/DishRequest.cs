using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Dish
{
    public class CreateDishRequest
    {
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        public decimal? Price { get; set; }

        [MaxLength(1000)]
        public string? Description { get; set; }

        public int? ImageId { get; set; }
    }

    public class UpdateDishRequest
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
