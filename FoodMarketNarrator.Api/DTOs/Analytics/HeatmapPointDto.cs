namespace food_market_narrator_api.DTOs.Analytics;

public class HeatmapPointDto
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
}

public class HeatmapResponse
{
    public List<HeatmapPointDto> Points { get; set; } = [];
    public int Count { get; set; }
}
