using MongoDB.Bson;
using MongoDB.Driver;

namespace food_market_narrator_api.Repositories;

public class UserSessionRepository
{
    private readonly IMongoCollection<BsonDocument> _userSessions;

    public UserSessionRepository(IMongoDatabase mongoDatabase)
    {
        _userSessions = mongoDatabase.GetCollection<BsonDocument>("UserSessions");
    }

    public async Task UpsertStartAsync(UserSessionStartRecord record)
    {
        var now = DateTime.UtcNow;
        var filter = Builders<BsonDocument>.Filter.Eq("device_id", record.DeviceId);

        var update = Builders<BsonDocument>.Update
            .Set("session_id", record.SessionId)
            .Set("device_id", record.DeviceId)
            .Set("device_info", record.DeviceInfo)
            .Set("last_seen_at", now)
            .Set("updated_at", now)
            .SetOnInsert("created_at", now);

        await _userSessions.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public async Task TouchSessionsAsync(IReadOnlyCollection<string> sessionIds, DateTime lastSeenAtUtc)
    {
        if (sessionIds.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var filter = Builders<BsonDocument>.Filter.In("session_id", sessionIds);
        var update = Builders<BsonDocument>.Update
            .Set("last_seen_at", lastSeenAtUtc)
            .Set("updated_at", now);

        await _userSessions.UpdateManyAsync(filter, update);
    }
}

public class UserSessionStartRecord
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
}
