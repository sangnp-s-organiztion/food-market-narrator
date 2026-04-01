namespace food_market_narrator_api.DTOs.Analytics;

public class KpiResponse
{
    public int TotalUsers { get; set; }
    public double AverageListeningTimeSeconds { get; set; }
    public string AverageListeningTimeFormatted { get; set; } = string.Empty;
    public int TotalPoiPlays { get; set; }
}
