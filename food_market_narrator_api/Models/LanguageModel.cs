using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models
{
    [Table("Languages")]
    public class LanguageModel
    {
        [Key]
        [Column("language_id")]
        public int LanguageId { get; set; }

        [Required]
        [Column("language_name")]
        [MaxLength(100)]
        public string LanguageName { get; set; } = string.Empty;

        [Required]
        [Column("language_code")]
        [MaxLength(10)]
        public string LanguageCode { get; set; } = string.Empty;
    }
}
