using MongoDB.Bson;
using MongoDB.Driver;

namespace food_market_narrator_api.Repositories;

public class AudioLogRepository
{
    private readonly IMongoCollection<BsonDocument> _audioLogs;

    public AudioLogRepository(IMongoDatabase mongoDatabase)
    {
        _audioLogs = mongoDatabase.GetCollection<BsonDocument>("AudioLogs");
    }

    public async Task InsertAsync(AudioLogRecord record)
    {
        var doc = new BsonDocument
        {
            { "session_id", record.SessionObjectId },
            { "restaurant_id", record.RestaurantId },
            { "audio_id", record.AudioId },
            { "start_time", record.StartTimeUtc },
            { "end_time", record.EndTimeUtc },
            { "duration", record.DurationSeconds },
            { "created_at", DateTime.UtcNow }
        };

        await _audioLogs.InsertOneAsync(doc);
    }
}

public class AudioLogRecord
{
    public ObjectId SessionObjectId { get; set; }
    public string RestaurantId { get; set; } = string.Empty;
    public int AudioId { get; set; }
    public DateTime StartTimeUtc { get; set; }
    public DateTime EndTimeUtc { get; set; }
    public int DurationSeconds { get; set; }
}
