using food_market_narrator_api.DTOs.Tour;
using food_market_narrator_api.Models;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services;

public class TourService
{
    private readonly TourRepository _tourRepository;

    public TourService(TourRepository tourRepository)
    {
        _tourRepository = tourRepository;
    }

    public async Task<List<TourResponse>> GetAllToursAsync(double? latitude = null, double? longitude = null, double radiusMeters = 30)
    {
        var tours = await _tourRepository.GetAllAsync();

        return tours
            .Select(t => MapTour(t, latitude, longitude, radiusMeters))
            .OrderByDescending(t => t.NearbyStopCount)
            .ThenBy(t => t.NearestDistanceMeters ?? double.MaxValue)
            .ThenByDescending(t => t.IsFeatured)
            .ThenByDescending(t => t.SortPriority)
            .ThenBy(t => t.Name)
            .ToList();
    }

    public async Task<TourResponse?> GetTourByIdAsync(int id, double? latitude = null, double? longitude = null, double radiusMeters = 30)
    {
        var tour = await _tourRepository.GetByIdAsync(id);
        if (tour == null)
        {
            return null;
        }

        return MapTour(tour, latitude, longitude, radiusMeters);
    }

    public async Task<AddTourRestaurantResult> AddRestaurantToTourAsync(int tourId, string restaurantId)
    {
        var normalizedRestaurantId = restaurantId.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRestaurantId))
        {
            return AddTourRestaurantResult.Invalid("restaurantId is required.");
        }

        var tourExists = await _tourRepository.ExistsAsync(tourId, includeInactive: true);
        if (!tourExists)
        {
            return AddTourRestaurantResult.NotFound("Tour not found.");
        }

        var restaurantExists = await _tourRepository.RestaurantExistsAsync(normalizedRestaurantId);
        if (!restaurantExists)
        {
            return AddTourRestaurantResult.NotFound("Restaurant not found.");
        }

        var mappingExists = await _tourRepository.TourRestaurantExistsAsync(tourId, normalizedRestaurantId);
        if (mappingExists)
        {
            return AddTourRestaurantResult.Conflict("Restaurant already exists in this tour.");
        }

        var effectiveStopOrder = await _tourRepository.GetNextStopOrderAsync(tourId);

        await _tourRepository.AddRestaurantToTourAsync(tourId, normalizedRestaurantId, effectiveStopOrder);
        return AddTourRestaurantResult.Success();
    }

    public async Task<ReorderTourStopsResult> ReorderTourStopsAsync(int tourId, IReadOnlyList<string> orderedRestaurantIds)
    {
        if (orderedRestaurantIds == null || orderedRestaurantIds.Count == 0)
        {
            return ReorderTourStopsResult.Invalid("restaurantIds is required.");
        }

        var tourExists = await _tourRepository.ExistsAsync(tourId, includeInactive: true);
        if (!tourExists)
        {
            return ReorderTourStopsResult.NotFound("Tour not found.");
        }

        var normalizedIds = orderedRestaurantIds
            .Select(id => id?.Trim() ?? string.Empty)
            .ToList();

        if (normalizedIds.Any(string.IsNullOrWhiteSpace))
        {
            return ReorderTourStopsResult.Invalid("restaurantIds contains invalid value.");
        }

        var hasDuplicates = normalizedIds.Count != normalizedIds.Distinct(StringComparer.OrdinalIgnoreCase).Count();
        if (hasDuplicates)
        {
            return ReorderTourStopsResult.Invalid("restaurantIds contains duplicated values.");
        }

        var currentIds = await _tourRepository.GetTourRestaurantIdsAsync(tourId);
        if (currentIds.Count != normalizedIds.Count)
        {
            return ReorderTourStopsResult.Invalid("restaurantIds must contain all stops in the tour.");
        }

        var currentSet = new HashSet<string>(currentIds, StringComparer.OrdinalIgnoreCase);
        var incomingSet = new HashSet<string>(normalizedIds, StringComparer.OrdinalIgnoreCase);
        if (!currentSet.SetEquals(incomingSet))
        {
            return ReorderTourStopsResult.Invalid("restaurantIds must match current stops in the tour.");
        }

        await _tourRepository.ReorderStopsAsync(tourId, normalizedIds);
        return ReorderTourStopsResult.Success();
    }

    public async Task<UpdateTourResult> UpdateTourAsync(
        int tourId,
        int? estimatedDurationMinutes,
        int sortPriority,
        bool isFeatured)
    {
        if (estimatedDurationMinutes.HasValue && estimatedDurationMinutes.Value < 0)
        {
            return UpdateTourResult.Invalid("estimatedDurationMinutes must be greater than or equal to 0.");
        }

        if (sortPriority < 0)
        {
            return UpdateTourResult.Invalid("sortPriority must be greater than or equal to 0.");
        }

        var updated = await _tourRepository.UpdateTourMetadataAsync(
            tourId,
            estimatedDurationMinutes,
            sortPriority,
            isFeatured);

        if (!updated)
        {
            return UpdateTourResult.NotFound("Tour not found.");
        }

        return UpdateTourResult.Success();
    }

    private static TourResponse MapTour(TourModel tour, double? latitude, double? longitude, double radiusMeters)
    {
        var orderedStops = tour.TourRestaurants
            .OrderBy(tr => tr.StopOrder)
            .ToList();

        var stops = orderedStops.Select(tr => new TourStopResponse
        {
            StopOrder = tr.StopOrder,
            RestaurantId = tr.RestaurantId,
            RestaurantName = tr.Restaurant?.Name ?? string.Empty,
            Latitude = tr.Restaurant?.Latitude,
            Longitude = tr.Restaurant?.Longitude,
            Address = tr.Restaurant?.Address,
            PrimaryImageUrl = tr.Restaurant?.ImageURL
                .OrderByDescending(i => i.IsPrimary)
                .ThenBy(i => i.SortOrder)
                .Select(i => i.ImageUrl)
                .FirstOrDefault()
        }).ToList();

        var nearbyStopCount = 0;
        double? nearestDistanceMeters = null;

        if (latitude.HasValue && longitude.HasValue)
        {
            foreach (var stop in stops)
            {
                if (!stop.Latitude.HasValue || !stop.Longitude.HasValue)
                {
                    continue;
                }

                var distanceMeters = CalculateDistanceMeters(
                    latitude.Value,
                    longitude.Value,
                    (double)stop.Latitude.Value,
                    (double)stop.Longitude.Value);

                if (!nearestDistanceMeters.HasValue || distanceMeters < nearestDistanceMeters.Value)
                {
                    nearestDistanceMeters = distanceMeters;
                }

                if (distanceMeters <= radiusMeters)
                {
                    nearbyStopCount++;
                }
            }
        }

        var imageUrl = !string.IsNullOrWhiteSpace(tour.Image?.ImageUrl)
            ? tour.Image.ImageUrl
            : stops.Select(s => s.PrimaryImageUrl).FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

        return new TourResponse
        {
            TourId = tour.TourId,
            Name = tour.Name,
            ShortDescription = tour.ShortDescription,
            Description = tour.Description,
            EstimatedDurationMinutes = tour.EstimatedDurationMinutes,
            ImageUrl = imageUrl,
            IsFeatured = tour.IsFeatured,
            SortPriority = tour.SortPriority,
            StopCount = stops.Count,
            NearbyStopCount = nearbyStopCount,
            NearestDistanceMeters = nearestDistanceMeters,
            Stops = stops
        };
    }

    private static double CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;

        var dLat = DegreesToRadians(lat2 - lat1);
        var dLon = DegreesToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
                + Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2))
                * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusMeters * c;
    }

    private static double DegreesToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}

public enum AddTourRestaurantStatus
{
    Success,
    NotFound,
    Conflict,
    Invalid
}

public class AddTourRestaurantResult
{
    public AddTourRestaurantStatus Status { get; private set; }
    public string? Message { get; private set; }

    public static AddTourRestaurantResult Success() => new() { Status = AddTourRestaurantStatus.Success };

    public static AddTourRestaurantResult NotFound(string message) =>
        new() { Status = AddTourRestaurantStatus.NotFound, Message = message };

    public static AddTourRestaurantResult Conflict(string message) =>
        new() { Status = AddTourRestaurantStatus.Conflict, Message = message };

    public static AddTourRestaurantResult Invalid(string message) =>
        new() { Status = AddTourRestaurantStatus.Invalid, Message = message };
}

public enum ReorderTourStopsStatus
{
    Success,
    NotFound,
    Invalid
}

public class ReorderTourStopsResult
{
    public ReorderTourStopsStatus Status { get; private set; }
    public string? Message { get; private set; }

    public static ReorderTourStopsResult Success() => new() { Status = ReorderTourStopsStatus.Success };

    public static ReorderTourStopsResult NotFound(string message) =>
        new() { Status = ReorderTourStopsStatus.NotFound, Message = message };

    public static ReorderTourStopsResult Invalid(string message) =>
        new() { Status = ReorderTourStopsStatus.Invalid, Message = message };
}

public enum UpdateTourStatus
{
    Success,
    NotFound,
    Invalid
}

public class UpdateTourResult
{
    public UpdateTourStatus Status { get; private set; }
    public string? Message { get; private set; }

    public static UpdateTourResult Success() => new() { Status = UpdateTourStatus.Success };

    public static UpdateTourResult NotFound(string message) =>
        new() { Status = UpdateTourStatus.NotFound, Message = message };

    public static UpdateTourResult Invalid(string message) =>
        new() { Status = UpdateTourStatus.Invalid, Message = message };
}
