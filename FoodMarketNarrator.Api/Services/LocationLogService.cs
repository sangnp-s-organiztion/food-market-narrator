using food_market_narrator_api.DTOs.Mongo;
using food_market_narrator_api.Repositories;

namespace food_market_narrator_api.Services;

public class LocationLogService
{
    private readonly LocationLogRepository _locationLogRepository;

    public LocationLogService(LocationLogRepository locationLogRepository)
    {
        _locationLogRepository = locationLogRepository;
    }

    public async Task<int> WriteBatchAsync(LocationLogBatchRequest request)
    {
        if (request.Items.Count == 0)
        {
            return 0;
        }

        var records = new List<LocationLogRecord>();
        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.SessionId))
            {
                continue;
            }

            var timestamp = item.Timestamp == default
                ? DateTime.UtcNow
                : DateTime.SpecifyKind(item.Timestamp, DateTimeKind.Utc);

            var (lng, lat) = ExtractCoordinates(item.Location);
            records.Add(new LocationLogRecord
            {
                SessionId = item.SessionId.Trim(),
                Timestamp = timestamp,
                Longitude = lng,
                Latitude = lat
            });
        }

        await _locationLogRepository.InsertBatchAsync(records);
        return records.Count;
    }

    private static (double? Longitude, double? Latitude) ExtractCoordinates(GeoPointRequest? location)
    {
        if (location == null)
        {
            return (null, null);
        }

        if (!string.Equals(location.Type, "Point", StringComparison.OrdinalIgnoreCase))
        {
            return (null, null);
        }

        if (location.Coordinates.Count < 2)
        {
            return (null, null);
        }

        var lng = location.Coordinates[0];
        var lat = location.Coordinates[1];

        if (!lng.HasValue || !lat.HasValue)
        {
            return (null, null);
        }

        return (lng.Value, lat.Value);
    }
}
