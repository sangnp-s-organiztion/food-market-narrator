using System.ComponentModel.DataAnnotations;

namespace food_market_narrator_api.DTOs.Mongo;

public class LocationLogBatchRequest
{
    [Required]
    public List<LocationLogItemRequest> Items { get; set; } = [];
}

public class LocationLogItemRequest
{
    [Required]
    public string SessionId { get; set; } = string.Empty;

    public DateTime Timestamp { get; set; }

    public GeoPointRequest? Location { get; set; }
}

public class GeoPointRequest
{
    [Required]
    public string Type { get; set; } = "Point";

    public List<double?> Coordinates { get; set; } = [];
}
