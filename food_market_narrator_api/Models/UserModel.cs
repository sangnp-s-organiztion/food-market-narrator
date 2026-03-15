using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System;

namespace food_market_narrator_api.Models
{
    [Table("Users")]
    public class UserModel
    {
        [Key]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("username")]
        [MaxLength(255)]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("password_hash")]
        [MaxLength(512)]
        public string PasswordHash { get; set; } = string.Empty;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
