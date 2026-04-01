namespace food_market_narrator_api.DTOs.Analytics;

public class TopAudioDto
{
    public int AudioId { get; set; }
    public string? AudioUrl { get; set; }
    public string? RestaurantId { get; set; }
    public string? RestaurantName { get; set; }
    public string? LanguageName { get; set; }
    public int PlayCount { get; set; }
    public double AverageDurationSeconds { get; set; }
    public string AverageDurationFormatted { get; set; } = string.Empty;
}

public class TopAudiosResponse
{
    public List<TopAudioDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
