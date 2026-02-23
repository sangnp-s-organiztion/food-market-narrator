using SQLite;
using Microsoft.Maui.Controls.Maps;
using System.Linq;

namespace food_market_narrator.Models;

public class POI
{
    [PrimaryKey]
    public string restaurantId { get; set; } = string.Empty;

    public string? Name { get; set; }
    public string? Description { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Category { get; set; }
    public double Radius { get; set; } = 500; // met
    public bool IsActive { get; set; }

    [Ignore]
    public string StatusText => IsActive ? "Đang mở cửa" : "Đóng cửa";
    
    public DateTime CreatedAt { get; set; }
    public string AudioFile { get; set; } = string.Empty;
    public List<RestaurantImageModel> Images { get; set; } = new();
    public List<AudioModel> Audios { get; set; } = new();
    
    public Pin? MapPin { get; set; } 

    
    // Thong tin bo sung
    public string? PriceRange { get; set; }
    public string? OpeningHours { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    
    // Đường dẫn file âm thanh offline (nếu có)
    public string? AudioFilePath { get; set; } 

    [Ignore]
    public string PrimaryImage
    {
        get
        {
            var selected = Images
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.ImageUrl)
                .FirstOrDefault();

            if (string.IsNullOrWhiteSpace(selected))
            {
                return "dotnet_bot.svg";
            }

            return selected
                .Replace("Resources/Images/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Replace("resources/images/", string.Empty, StringComparison.OrdinalIgnoreCase)
                .Trim();
        }
    }

    public string? GetAudioUrl(string languageCode)
    {
        var activeAudios = Audios
            .Where(a => a.IsActive)
            .ToList();

        var byLanguage = activeAudios
            .Where(a => string.Equals(a.LanguageCode, languageCode, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(a => a.Version)
            .ThenByDescending(a => a.DateGeneration)
            .FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(byLanguage?.AudioUrl))
        {
            return byLanguage.AudioUrl;
        }

        return activeAudios
            .OrderByDescending(a => a.Version)
            .ThenByDescending(a => a.DateGeneration)
            .Select(a => a.AudioUrl)
            .FirstOrDefault();
    }
}
