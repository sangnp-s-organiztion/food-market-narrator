namespace food_market_narrator.Models;

public class AudioModel
{
    public int AudioId { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public int LanguageId { get; set; }
    public string LanguageName { get; set; } = string.Empty;
    public string LanguageCode { get; set; } = string.Empty;
    public string AudioUrl { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public bool IsActive { get; set; } = true;
    public DateTime DateGeneration { get; set; } = DateTime.Now;
}
