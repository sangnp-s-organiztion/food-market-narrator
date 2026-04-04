using MongoDB.Bson;
using MongoDB.Driver;
using Microsoft.Extensions.Logging;

namespace food_market_narrator_api.Repositories;

public class LocationLogRepository
{
    private readonly IMongoCollection<BsonDocument> _locationLogs;
    private readonly ILogger<LocationLogRepository> _logger;

    public LocationLogRepository(IMongoDatabase mongoDatabase, ILogger<LocationLogRepository> logger)
    {
        _locationLogs = mongoDatabase.GetCollection<BsonDocument>("LocationLogs");
        _logger = logger;
    }

    public async Task InsertBatchAsync(List<LocationLogRecord> records)
    {
        if (records.Count == 0)
        {
            return;
        }

        var docs = records.Select(r =>
        {
            var doc = new BsonDocument
            {
                { "session_id", r.SessionId },
                { "timestamp", r.Timestamp }
            };

            if (r.Longitude.HasValue && r.Latitude.HasValue)
            {
                doc["location"] = new BsonDocument
                {
                    { "type", "Point" },
                    { "coordinates", new BsonArray { r.Longitude.Value, r.Latitude.Value } }
                };
            }
            else
            {
                doc["location"] = BsonNull.Value;
            }

            return doc;
        }).ToList();

        await _locationLogs.InsertManyAsync(docs);
        _logger.LogInformation("Sync log to server: inserted {Count} location points to MongoDB", docs.Count);
    }
}

public class LocationLogRecord
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
}
