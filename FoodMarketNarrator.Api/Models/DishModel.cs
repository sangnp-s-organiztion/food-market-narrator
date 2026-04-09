using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models
{
    [Table("Dish")]
    public class DishModel
    {
        [Key]
        [Column("dish_id")]
        public int DishId { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [Column("price", TypeName = "decimal(10,2)")]
        public decimal? Price { get; set; }

        [Column("created_at")]
        public DateTime? CreatedAt { get; set; }

        [Required]
        [Column("restaurant_id")]
        [MaxLength(100)]
        public string RestaurantId { get; set; } = string.Empty;

        [Column("is_active")]
        public bool? IsActive { get; set; }

        [Column("image_id")]
        public int? ImageId { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public RestaurantModel? Restaurant { get; set; }

        [ForeignKey(nameof(ImageId))]
        public RestaurantImageModel? Image { get; set; }
    }
}
