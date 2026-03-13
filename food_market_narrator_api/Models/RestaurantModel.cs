using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;
using System.Collections.Generic;


namespace food_market_narrator_api.Models
{
    [Table("Restaurant")] // nếu tên table là Restaurants
    public class RestaurantModel

    {
        [Key]
        [Column("restaurant_id")]
        [MaxLength(255)]
        public string RestaurantId { get; set; }

        [Required]
        [Column("name")]
        [MaxLength(255)]
        public string Name { get; set; }

        [Column("description")]
        [MaxLength(1000)]
        public string? Description { get; set; }

        [Column("latitude", TypeName = "decimal(9,6)")]
        public decimal? Latitude { get; set; }

        [Column("longitude", TypeName = "decimal(9,6)")]
        public decimal? Longitude { get; set; }

        [Column("address")]
        [MaxLength(500)]
        public string? Address { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        [Column("user_id")]
        public int UserId { get; set; }

        [ForeignKey(nameof(UserId))]
        public UserModel User { get; set; }

        // Navigation properties
        public ICollection<RestaurantImageModel> ImageURL { get; set; } = new List<RestaurantImageModel>();
        public ICollection<AudioModel> AudioURL { get; set; } = new List<AudioModel>();
    }
}
