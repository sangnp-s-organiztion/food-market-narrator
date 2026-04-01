namespace food_market_narrator_api.DTOs.Analytics;

public class MovementPathDto
{
    public string SessionId { get; set; } = string.Empty;
    public List<MovementPointDto> Points { get; set; } = [];
}

public class MovementPointDto
{
    public double Longitude { get; set; }
    public double Latitude { get; set; }
    public DateTime Timestamp { get; set; }
}

public class MovementPathsResponse
{
    public List<MovementPathDto> Sessions { get; set; } = [];
    public int TotalSessions { get; set; }
}
