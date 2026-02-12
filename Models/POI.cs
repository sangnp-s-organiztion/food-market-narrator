using SQLite;
using Microsoft.Maui.Controls.Maps;

namespace food_market_narrator.Models;

public class POI
{
    [PrimaryKey]
    public string Id { get; set; } = string.Empty;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Category { get; set; }
    public double Radius { get; set; } = 500; // met
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    public Pin? MapPin { get; set; } 

    
    // Thong tin bo sung
    public string? PriceRange { get; set; }
    public string? OpeningHours { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    
    // Đường dẫn file âm thanh offline (nếu có)
    public string? AudioFilePath { get; set; } 

    public double GetDistance(double userLat, double userLng)
    {
        const double R = 6371000; // ban kinh trai dat (met)
        var lat1 = ToRadians(userLat);
        var lat2 = ToRadians(Latitude);
        var deltaLat = ToRadians(Latitude - userLat);
        var deltaLng = ToRadians(Longitude - userLng);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1) * Math.Cos(lat2) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    public bool IsNearby(double userLat, double userLng)
    {
        return GetDistance(userLat, userLng) <= Radius;
    }
}
