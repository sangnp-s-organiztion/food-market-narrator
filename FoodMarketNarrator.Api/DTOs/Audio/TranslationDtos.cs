using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Audio;

public class TranslateTextRequest
{
    [Required]
    [MaxLength(8000)]
    public string Text { get; set; } = string.Empty;

    [MaxLength(10)]
    public string? SourceLanguageCode { get; set; }

    [Required]
    [MaxLength(10)]
    public string TargetLanguageCode { get; set; } = string.Empty;

    [MaxLength(64)]
    public string? RequestId { get; set; }
}

public class TranslateTextResponse
{
    public string RequestId { get; set; } = string.Empty;
    public string SourceLanguageCode { get; set; } = string.Empty;
    public string TargetLanguageCode { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public int InputChars { get; set; }
    public int OutputChars { get; set; }
    public decimal EstimatedCost { get; set; }
    public string Currency { get; set; } = "USD";
}

public class CreateAudioFromTextRequest
{
    [Required]
    [MaxLength(8000)]
    public string Text { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string LanguageCode { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? Voice { get; set; }

    [MaxLength(64)]
    public string? RequestId { get; set; }

    [MaxLength(8000)]
    public string? SourceText { get; set; }
}

public class CreateAudioFromTextResponse
{
    public string RequestId { get; set; } = string.Empty;
    public int AudioId { get; set; }
    public string AudioUrl { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string Voice { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
