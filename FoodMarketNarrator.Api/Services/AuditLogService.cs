using food_market_narrator_api.DTOs.AuditLog;
using food_market_narrator_api.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace food_market_narrator_api.Services;

public class AuditLogService
{
    private readonly IMongoCollection<BsonDocument> _auditLogs;
    private readonly ILogger<AuditLogService> _logger;

    public AuditLogService(IMongoDatabase mongoDatabase, ILogger<AuditLogService> logger)
    {
        _auditLogs = mongoDatabase.GetCollection<BsonDocument>("AuditLogs");
        _logger = logger;
    }

    public async Task<(List<AuditLogResponse> Items, int TotalCount)> GetLogsAsync(
        int page = 1,
        int pageSize = 20,
        int? userId = null,
        string? action = null,
        string? targetType = null,
        DateTime? from = null,
        DateTime? to = null)
    {
        var filters = new List<FilterDefinition<BsonDocument>>();

        if (userId.HasValue)
            filters.Add(Builders<BsonDocument>.Filter.Eq("user_id", userId.Value));
        if (!string.IsNullOrWhiteSpace(action))
            filters.Add(Builders<BsonDocument>.Filter.Eq("action", action));
        if (!string.IsNullOrWhiteSpace(targetType))
            filters.Add(Builders<BsonDocument>.Filter.Eq("target_type", targetType));
        if (from.HasValue)
            filters.Add(Builders<BsonDocument>.Filter.Gte("created_at", from.Value));
        if (to.HasValue)
            filters.Add(Builders<BsonDocument>.Filter.Lte("created_at", to.Value));

        var finalFilter = filters.Count > 0
            ? Builders<BsonDocument>.Filter.And(filters)
            : FilterDefinition<BsonDocument>.Empty;

        var totalCountLong = await _auditLogs.CountDocumentsAsync(finalFilter);
        var totalCount = totalCountLong > int.MaxValue ? int.MaxValue : (int)totalCountLong;

        var docs = await _auditLogs.Find(finalFilter)
            .Sort(Builders<BsonDocument>.Sort.Descending("created_at"))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync();

        var items = docs.Select(d => new AuditLogResponse
        {
            Id = 0,
            UserId = d.GetValue("user_id", BsonValue.Create(0)).ToInt32(),
            Username = d.GetValue("username", string.Empty).AsString,
            Action = d.GetValue("action", string.Empty).AsString,
            TargetType = d.GetValue("target_type", string.Empty).AsString,
            TargetId = d.GetValue("target_id", BsonNull.Value).IsBsonNull ? null : d["target_id"].AsString,
            Details = d.GetValue("details", BsonNull.Value).IsBsonNull ? null : d["details"].AsString,
            IpAddress = d.GetValue("ip_address", BsonNull.Value).IsBsonNull ? null : d["ip_address"].AsString,
            CreatedAt = d.GetValue("created_at", BsonDateTime.Create(DateTime.UtcNow)).ToUniversalTime()
        }).ToList();

        return (items, totalCount);
    }

    public async Task WriteLogAsync(AuditLog log)
    {
        var doc = new BsonDocument
        {
            { "user_id", log.UserId },
            { "username", log.Username ?? string.Empty },
            { "action", log.Action ?? string.Empty },
            { "target_type", log.TargetType ?? string.Empty },
            { "target_id", log.TargetId != null ? (BsonValue)log.TargetId : BsonNull.Value },
            { "details", log.Details != null ? (BsonValue)log.Details : BsonNull.Value },
            { "ip_address", log.IpAddress != null ? (BsonValue)log.IpAddress : BsonNull.Value },
            { "created_at", log.CreatedAt == default ? DateTime.UtcNow : log.CreatedAt }
        };

        try
        {
            await _auditLogs.InsertOneAsync(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log to MongoDB");
        }
    }
}
