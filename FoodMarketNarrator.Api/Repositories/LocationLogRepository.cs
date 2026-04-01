using MongoDB.Bson;
using MongoDB.Driver;

namespace food_market_narrator_api.Repositories;

public class LocationLogRepository
{
    private readonly IMongoCollection<BsonDocument> _locationLogs;

    public LocationLogRepository(IMongoDatabase mongoDatabase)
    {
        _locationLogs = mongoDatabase.GetCollection<BsonDocument>("LocationLogs");
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
    }
}

public class LocationLogRecord
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public double? Longitude { get; set; }
    public double? Latitude { get; set; }
}
