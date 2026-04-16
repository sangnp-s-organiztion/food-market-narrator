namespace food_market_narrator_api.DTOs.Tour;

public class TourResponse
{
    public int TourId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public int StopCount { get; set; }
    public int NearbyStopCount { get; set; }
    public double? NearestDistanceMeters { get; set; }
    public List<TourStopResponse> Stops { get; set; } = new();
}

public class TourStopResponse
{
    public int StopOrder { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public string? PrimaryImageUrl { get; set; }
}
