using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models
{
    [Table("Audio")]
    public class AudioModel
    {
        [Key]
        [Column("audio_id")]
        public int AudioId { get; set; }

        [Required]
        [Column("restaurant_id")]
        [MaxLength(255)]
        public string RestaurantId { get; set; } = string.Empty;

        [Column("language_id")]
        public int LanguageId { get; set; }

        [Required]
        [Column("audio_url")]
        [MaxLength(500)]
        public string AudioUrl { get; set; } = string.Empty;

        [Column("version")]
        public int Version { get; set; } = 1;

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("date_generation")]
        public DateTime DateGeneration { get; set; } = DateTime.Now;

        [ForeignKey(nameof(RestaurantId))]
        public RestaurantModel? Restaurant { get; set; }

        [ForeignKey(nameof(LanguageId))]
        public LanguageModel? Language { get; set; }
    }
}
