namespace food_market_narrator_api.DTOs.Analytics;

public class TopRestaurantDto
{
    public string RestaurantId { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public int PlayCount { get; set; }
    public double AverageDurationSeconds { get; set; }
    public string AverageDurationFormatted { get; set; } = string.Empty;
}

public class TopRestaurantsResponse
{
    public List<TopRestaurantDto> Items { get; set; } = [];
    public int TotalCount { get; set; }
}
