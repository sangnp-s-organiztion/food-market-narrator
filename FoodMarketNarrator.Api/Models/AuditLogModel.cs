using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models
{
    [Table("AuditLogs")]
    public class AuditLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Column("action")]
        public string Action { get; set; } = string.Empty;

        [Required]
        [Column("target_type")]
        public string TargetType { get; set; } = string.Empty;

        [Column("target_id")]
        public string? TargetId { get; set; }

        [Column("details")]
        public string? Details { get; set; }

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
