namespace food_market_narrator.Models;

public class TourModel
{
    private List<TourStopModel>? _stops;

    public int TourId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ShortDescription { get; set; }
    public string? Description { get; set; }
    public int? EstimatedDurationMinutes { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsFeatured { get; set; }
    public int SortPriority { get; set; }
    public int StopCount { get; set; }
    public int NearbyStopCount { get; set; }
    public double? NearestDistanceMeters { get; set; }
    public List<TourStopModel> Stops
    {
        get => _stops ??= new List<TourStopModel>();
        set => _stops = value ?? new List<TourStopModel>();
    }

    public string? ResolvedImageUrl { get; set; }

    public string DisplayImageSource => string.IsNullOrWhiteSpace(ResolvedImageUrl)
        ? "dotnet_bot.svg"
        : ResolvedImageUrl;

    public string DurationDisplay => EstimatedDurationMinutes.HasValue
        ? $"{EstimatedDurationMinutes.Value} PHÚT"
        : "ĐANG CẬP NHẬT";

    public string DurationCompactDisplay => EstimatedDurationMinutes.HasValue
        ? $"{EstimatedDurationMinutes.Value}p"
        : "--";

    public string StopCountDisplay => $"{StopCount} ĐIỂM DỪNG";

    public string BadgeText
    {
        get
        {
            if (NearbyStopCount > 0)
            {
                return $"{NearbyStopCount} gần";
            }

            return IsFeatured ? "Nổi bật" : "4.9";
        }
    }
}

public class TourStopModel
{
    public int StopOrder { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public string RestaurantName { get; set; } = string.Empty;
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? Address { get; set; }
    public string? PrimaryImageUrl { get; set; }
}
