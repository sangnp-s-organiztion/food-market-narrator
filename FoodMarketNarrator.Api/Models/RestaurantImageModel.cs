using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models
{
    [Table("Restaurant_Image")]
    public class RestaurantImageModel
    {
        [Key]
        [Column("image_id")]
        public int ImageId { get; set; }

        [Required]
        [Column("restaurant_id")]
        [MaxLength(255)]
        public string RestaurantId { get; set; } = string.Empty;

        [Required]
        [Column("image_url")]
        [MaxLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [Column("is_primary")]
        public bool IsPrimary { get; set; }

        [Column("sort_order")]
        public int SortOrder { get; set; }

        [ForeignKey(nameof(RestaurantId))]
        public RestaurantModel? Restaurant { get; set; }
    }
}
