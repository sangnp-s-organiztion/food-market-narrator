using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Language;

public class LanguageResponse
{
    public string LanguageCode { get; set; } = string.Empty;
    public string LanguageName { get; set; } = string.Empty;
}