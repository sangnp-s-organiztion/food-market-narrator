namespace food_market_narrator_api.Models;

public class LibreTranslateSettings
{
    public string BaseUrl { get; set; } = "http://localhost:5000";
    public string TranslatePath { get; set; } = "/translate";
    public int TimeoutSeconds { get; set; } = 30;
}

public class EdgeTtsSettings
{
    public string BaseUrl { get; set; } = "http://localhost:6000";
    public string SynthesizePath { get; set; } = "/synthesize";
    public int TimeoutSeconds { get; set; } = 90;
}

public class TranslationPricingSettings
{
    public decimal PricePer1KChars { get; set; } = 0.02m;
    public string Currency { get; set; } = "USD";
    public string RateVersion { get; set; } = "v1";
}
