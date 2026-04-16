using SQLite;
using System.Globalization;
using food_market_narrator;

namespace food_market_narrator.Models;

public class POI
{
    [PrimaryKey]
    public string restaurantId { get; set; } = string.Empty;

    public string? Name { get; set; }
    public string? Description { get; set; }

    [Ignore]
    public string? OriginalName { get; set; }

    [Ignore]
    public string? OriginalDescription { get; set; }

    [Ignore]
    public string? OriginalAddress { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string? Category { get; set; }
    public double Radius { get; set; } = 500; // met
    public bool IsActive { get; set; }

    [Ignore]
    public bool IsCurrentlyOpen
    {
        get
        {
            var now = DateTime.Now.TimeOfDay;
            if (!TryGetOpeningWindow(out var openTime, out var closeTime))
            {
                // Không có dữ liệu giờ mở/đóng thì fallback theo trạng thái hoạt động chung.
                return IsActive;
            }

            // So sánh với giờ hiện tại
            if (closeTime > openTime)
                return now >= openTime && now <= closeTime;
            else
                // Trường hợp đóng sau nửa đêm (vd: 18:00 - 02:00)
                return now >= openTime || now <= closeTime;
        }
    }

    [Ignore]
    public string StatusText => IsCurrentlyOpen
        ? LocalizationResourceManager.Instance["StatusOpenNow"]
        : LocalizationResourceManager.Instance["StatusClosedNow"];
    
    public DateTime CreatedAt { get; set; }
    public string AudioFile { get; set; } = string.Empty;
    public List<RestaurantImageModel> Images { get; set; } = new();
    public List<AudioModel> Audios { get; set; } = new();
    public List<DishModel> Dishes { get; set; } = new();


    
    // Thong tin bo sung
    public string? PriceRange { get; set; }
    public string? OpeningHours { get; set; }
    public TimeSpan? OpenTime { get; set; }
    public TimeSpan? CloseTime { get; set; }
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

    [Ignore]
    public string OpeningHoursDisplay
    {
        get
        {
            if (OpenTime.HasValue && CloseTime.HasValue)
            {
                return $"{FormatHour(OpenTime.Value)} - {FormatHour(CloseTime.Value)}";
            }

            if (!string.IsNullOrWhiteSpace(OpeningHours))
            {
                return OpeningHours;
            }

            return "Đang cập nhật";
        }
    }

    [Ignore]
    public string AddressDisplay => string.IsNullOrWhiteSpace(Address)
        ? "Đang cập nhật địa chỉ"
        : Address;

    [Ignore]
    public string CoordinatesDisplay => $"{Latitude.ToString("0.######", CultureInfo.InvariantCulture)}, {Longitude.ToString("0.######", CultureInfo.InvariantCulture)}";

    [Ignore]
    public string CreatedAtDisplay => CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss.fff", CultureInfo.InvariantCulture);

    [Ignore]
    public string AudioLanguagesDisplay
    {
        get
        {
            var names = Audios
                .Where(a => a.IsActive)
                .Select(a => string.IsNullOrWhiteSpace(a.LanguageName)
                    ? a.LanguageCode
                    : $"{a.LanguageName} ({a.LanguageCode})")
                .Distinct()
                .ToList();

            return names.Count == 0
                ? "Đang cập nhật"
                : string.Join(", ", names);
        }
    }

    [Ignore]
    public string AudioSummaryDisplay
    {
        get
        {
            var activeCount = Audios.Count(a => a.IsActive);
            return activeCount == 0
                ? "Audio: chưa có bản ghi"
                : $"Audio active: {activeCount} bản ghi";
        }
    }

    [Ignore]
    public string PrimaryDetailImage => NormalizeImagePath(Images
        .OrderByDescending(i => i.IsPrimary)
        .ThenBy(i => i.SortOrder)
        .Select(i => i.ImageUrl)
        .FirstOrDefault());

    [Ignore]
    public string SecondaryDetailImage => NormalizeImagePath(Images
        .OrderBy(i => i.SortOrder)
        .Select(i => i.ImageUrl)
        .Skip(1)
        .FirstOrDefault());

    [Ignore]
    public string ThirdDetailImage => NormalizeImagePath(Images
        .OrderBy(i => i.SortOrder)
        .Select(i => i.ImageUrl)
        .Skip(2)
        .FirstOrDefault() ?? PrimaryImage);

    private static string NormalizeImagePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return "dotnet_bot.svg";
        }

        return path
            .Replace("Resources/Images/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("resources/images/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();
    }

    private bool TryGetOpeningWindow(out TimeSpan openTime, out TimeSpan closeTime)
    {
        if (OpenTime.HasValue && CloseTime.HasValue)
        {
            openTime = OpenTime.Value;
            closeTime = CloseTime.Value;
            return true;
        }

        if (TryParseOpeningHoursText(OpeningHours, out openTime, out closeTime))
        {
            return true;
        }

        openTime = default;
        closeTime = default;
        return false;
    }

    private static bool TryParseOpeningHoursText(string? openingHours, out TimeSpan openTime, out TimeSpan closeTime)
    {
        if (string.IsNullOrWhiteSpace(openingHours))
        {
            openTime = default;
            closeTime = default;
            return false;
        }

        var normalized = openingHours
            .Replace('–', '-')
            .Replace('—', '-');

        var parts = normalized.Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            openTime = default;
            closeTime = default;
            return false;
        }

        if (!TryParseHourMinute(parts[0], out openTime) || !TryParseHourMinute(parts[1], out closeTime))
        {
            openTime = default;
            closeTime = default;
            return false;
        }

        return true;
    }

    private static bool TryParseHourMinute(string value, out TimeSpan time)
    {
        if (TimeSpan.TryParse(value, out time))
        {
            return true;
        }

        var hourMinute = value.Split(':', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (hourMinute.Length == 2
            && int.TryParse(hourMinute[0], out var hour)
            && int.TryParse(hourMinute[1], out var minute))
        {
            time = new TimeSpan(hour, minute, 0);
            return true;
        }

        time = default;
        return false;
    }

    private static string FormatHour(TimeSpan value) => $"{value.Hours:00}:{value.Minutes:00}";
}
