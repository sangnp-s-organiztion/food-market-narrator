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

        if (record.QrAccessExpiresAtUtc.HasValue)
        {
            var normalizedExpiry = DateTime.SpecifyKind(record.QrAccessExpiresAtUtc.Value, DateTimeKind.Utc);
            update = update.Min("qr_access_expires_at", normalizedExpiry);
        }

        await _userSessions.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });
    }

    public Task<long> CountVisitorsAsync()
    {
        return _userSessions.CountDocumentsAsync(FilterDefinition<BsonDocument>.Empty);
    }

    public async Task<IReadOnlyList<VisitorSessionRecord>> GetVisitorsAsync(int limit)
    {
        var normalizedLimit = Math.Clamp(limit, 1, 1000);

        var projection = Builders<BsonDocument>.Projection
            .Include("session_id")
            .Include("device_id")
            .Include("device_info")
            .Include("created_at")
            .Include("last_seen_at")
            .Include("updated_at")
            .Include("qr_access_expires_at");

        var sort = Builders<BsonDocument>.Sort
            .Descending("updated_at")
            .Descending("last_seen_at")
            .Descending("created_at");

        var docs = await _userSessions
            .Find(FilterDefinition<BsonDocument>.Empty)
            .Project(projection)
            .Sort(sort)
            .Limit(normalizedLimit)
            .ToListAsync();

        return docs.Select(MapVisitorRecord).ToList();
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

    public async Task<ObjectId?> FindObjectIdBySessionIdAsync(string sessionId)
    {
        var normalizedSessionId = (sessionId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedSessionId))
        {
            return null;
        }

        var filter = Builders<BsonDocument>.Filter.Eq("session_id", normalizedSessionId);
        var projection = Builders<BsonDocument>.Projection.Include("_id");

        var doc = await _userSessions
            .Find(filter)
            .Project(projection)
            .FirstOrDefaultAsync();

        if (doc == null || !doc.Contains("_id") || !doc["_id"].IsObjectId)
        {
            return null;
        }

        return doc["_id"].AsObjectId;
    }

    public async Task<UserSessionQrAccessRecord?> GetQrAccessBySessionIdAsync(string sessionId)
    {
        var normalizedSessionId = (sessionId ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedSessionId))
        {
            return null;
        }

        var filter = Builders<BsonDocument>.Filter.Eq("session_id", normalizedSessionId);
        var projection = Builders<BsonDocument>.Projection
            .Include("session_id")
            .Include("qr_access_expires_at");

        var doc = await _userSessions
            .Find(filter)
            .Project(projection)
            .FirstOrDefaultAsync();

        if (doc == null)
        {
            return null;
        }

        DateTime? qrAccessExpiresAtUtc = null;
        if (doc.TryGetValue("qr_access_expires_at", out var expiryValue) && expiryValue.IsBsonDateTime)
        {
            qrAccessExpiresAtUtc = DateTime.SpecifyKind(expiryValue.AsBsonDateTime.ToUniversalTime(), DateTimeKind.Utc);
        }

        return new UserSessionQrAccessRecord
        {
            SessionId = normalizedSessionId,
            QrAccessExpiresAtUtc = qrAccessExpiresAtUtc
        };
    }

    private static VisitorSessionRecord MapVisitorRecord(BsonDocument doc)
    {
        return new VisitorSessionRecord
        {
            SessionId = GetStringValue(doc, "session_id"),
            DeviceId = GetStringValue(doc, "device_id"),
            DeviceInfo = GetStringValue(doc, "device_info"),
            CreatedAtUtc = GetDateTimeValue(doc, "created_at"),
            LastSeenAtUtc = GetDateTimeValue(doc, "last_seen_at"),
            UpdatedAtUtc = GetDateTimeValue(doc, "updated_at"),
            QrAccessExpiresAtUtc = GetDateTimeValue(doc, "qr_access_expires_at")
        };
    }

    private static string GetStringValue(BsonDocument doc, string fieldName)
    {
        if (!doc.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return string.Empty;
        }

        var text = value.IsString ? value.AsString : value.ToString();
        return text ?? string.Empty;
    }

    private static DateTime? GetDateTimeValue(BsonDocument doc, string fieldName)
    {
        if (!doc.TryGetValue(fieldName, out var value) || value.IsBsonNull)
        {
            return null;
        }

        if (value.IsBsonDateTime)
        {
            return DateTime.SpecifyKind(value.AsBsonDateTime.ToUniversalTime(), DateTimeKind.Utc);
        }

        return null;
    }
}

public class UserSessionStartRecord
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime? QrAccessExpiresAtUtc { get; set; }
}

public class UserSessionQrAccessRecord
{
    public string SessionId { get; set; } = string.Empty;
    public DateTime? QrAccessExpiresAtUtc { get; set; }
}

public class VisitorSessionRecord
{
    public string SessionId { get; set; } = string.Empty;
    public string DeviceId { get; set; } = string.Empty;
    public string DeviceInfo { get; set; } = string.Empty;
    public DateTime? CreatedAtUtc { get; set; }
    public DateTime? LastSeenAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
    public DateTime? QrAccessExpiresAtUtc { get; set; }
}
