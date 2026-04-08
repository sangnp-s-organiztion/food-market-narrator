using food_market_narrator.Models;
using food_market_narrator.Settings;
using System.Globalization;
using System.Net.Http.Json;

namespace food_market_narrator.Services;

public class TourService : ITourService
{
    private readonly HttpClient _httpClient;
    private readonly ILocationService _locationService;

    public TourService(HttpClient httpClient, ILocationService locationService)
    {
        _httpClient = httpClient;
        _locationService = locationService;
    }

    public async Task<List<TourModel>> GetToursAsync()
    {
        try
        {
            var location = _locationService.LastKnownLocation ?? await _locationService.GetCurrentLocationAsync();
            var endpoint = BuildTourEndpoint(location);

            var tours = await _httpClient.GetFromJsonAsync<List<TourModel>>(endpoint);
            if (tours == null)
            {
                return new List<TourModel>();
            }

            foreach (var tour in tours)
            {
                tour.ResolvedImageUrl = ResolveImageUrl(tour.ImageUrl, tour.Stops);
            }

            return tours;
        }
        catch
        {
            return new List<TourModel>();
        }
    }

    public async Task<TourModel?> GetTourByIdAsync(int tourId)
    {
        try
        {
            var location = _locationService.LastKnownLocation ?? await _locationService.GetCurrentLocationAsync();
            var endpoint = BuildTourDetailEndpoint(tourId, location);
            var tour = await _httpClient.GetFromJsonAsync<TourModel>(endpoint);
            if (tour == null)
            {
                return null;
            }

            tour.ResolvedImageUrl = ResolveImageUrl(tour.ImageUrl, tour.Stops);
            return tour;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildTourEndpoint(Location? location)
    {
        if (location == null)
        {
            return AppSettings.TourEndpoint;
        }

        var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
        var radius = AppSettings.PoiEnterRadiusMeters.ToString(CultureInfo.InvariantCulture);
        return $"{AppSettings.TourEndpoint}?latitude={lat}&longitude={lng}&radiusMeters={radius}";
    }

    private static string BuildTourDetailEndpoint(int tourId, Location? location)
    {
        if (location == null)
        {
            return $"{AppSettings.TourEndpoint}/{tourId}";
        }

        var lat = location.Latitude.ToString(CultureInfo.InvariantCulture);
        var lng = location.Longitude.ToString(CultureInfo.InvariantCulture);
        var radius = AppSettings.PoiEnterRadiusMeters.ToString(CultureInfo.InvariantCulture);
        return $"{AppSettings.TourEndpoint}/{tourId}?latitude={lat}&longitude={lng}&radiusMeters={radius}";
    }

    private string ResolveImageUrl(string? imageUrl, List<TourStopModel>? stops)
    {
        var source = imageUrl;
        if (string.IsNullOrWhiteSpace(source))
        {
            source = stops?
                .OrderBy(s => s.StopOrder)
                .Select(s => s.PrimaryImageUrl)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));
        }

        if (string.IsNullOrWhiteSpace(source))
        {
            return "dotnet_bot.svg";
        }

        if (Uri.TryCreate(source, UriKind.Absolute, out _))
        {
            return source;
        }

        if (source.StartsWith("/", StringComparison.Ordinal))
        {
            return $"{_httpClient.BaseAddress?.ToString().TrimEnd('/')}{source}";
        }

        if (source.Contains("/", StringComparison.Ordinal))
        {
            return source;
        }

        return source.Trim();
    }
}
