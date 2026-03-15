using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace food_market_narrator_api.Models
{
    [Table("UserRestaurant")]
    public class UserRestaurantModel
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("restaurant_id")]
        [MaxLength(255)]
        public string RestaurantId { get; set; } = string.Empty;

        [Column("role")]
        [MaxLength(50)]
        public string? Role { get; set; }
    }
}
