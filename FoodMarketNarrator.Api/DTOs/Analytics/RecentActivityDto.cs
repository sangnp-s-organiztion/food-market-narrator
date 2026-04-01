namespace food_market_narrator_api.DTOs.Analytics;

public class RecentActivityDto
{
    public int AudioId { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public string? RestaurantName { get; set; }
    public int Duration { get; set; }
    public DateTime Timestamp { get; set; }
}

public class RecentActivityResponse
{
    public List<RecentActivityDto> Items { get; set; } = [];
    public int Count { get; set; }
}
