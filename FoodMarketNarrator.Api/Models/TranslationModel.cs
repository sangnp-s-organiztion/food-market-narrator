using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models;

[Table("Translation")]
[Index(nameof(EntityType), nameof(EntityId), nameof(LanguageId), nameof(FieldName), IsUnique = true, Name = "UQ_translation")]
public class TranslationModel
{
    [Key]
    [Column("translation_id")]
    public int TranslationId { get; set; }

    [Required]
    [Column("entity_type")]
    [MaxLength(50)]
    public string EntityType { get; set; } = string.Empty;

    [Required]
    [Column("entity_id")]
    [MaxLength(100)]
    public string EntityId { get; set; } = string.Empty;

    [Required]
    [Column("language_id")]
    public int LanguageId { get; set; }

    [Required]
    [Column("field_name")]
    [MaxLength(50)]
    public string FieldName { get; set; } = string.Empty;

    [Required]
    [Column("translated_text", TypeName = "nvarchar(max)")]
    public string TranslatedText { get; set; } = string.Empty;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [ForeignKey(nameof(LanguageId))]
    public LanguageModel? Language { get; set; }
}
