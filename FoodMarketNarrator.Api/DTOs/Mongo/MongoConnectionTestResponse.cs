namespace food_market_narrator_api.DTOs.Mongo;

public class MongoConnectionTestResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? DatabaseName { get; set; }
    public DateTime CheckedAtUtc { get; set; }
}
