using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Language;

public class LanguageRequest
{
    [Required]
    public string LanguageCode { get; set; } = string.Empty;
}