using food_market_narrator_api.Data.Context;
using food_market_narrator_api.DTOs.AuditLog;
using Microsoft.EntityFrameworkCore;

namespace food_market_narrator_api.Services;

public class AuditLogService
{
    private readonly AppDbContext _db;

    public AuditLogService(AppDbContext db)
    {
        _db = db;
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
        var query = _db.AuditLogs.AsQueryable();

        if (userId.HasValue)
            query = query.Where(l => l.UserId == userId.Value);
        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(l => l.Action == action);
        if (!string.IsNullOrWhiteSpace(targetType))
            query = query.Where(l => l.TargetType == targetType);
        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);
        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogResponse
            {
                Id = l.Id,
                UserId = l.UserId,
                Username = l.Username,
                Action = l.Action,
                TargetType = l.TargetType,
                TargetId = l.TargetId,
                TargetName = l.TargetName,
                Details = l.Details,
                IpAddress = l.IpAddress,
                CreatedAt = l.CreatedAt
            })
            .ToListAsync();

        return (items, totalCount);
    }
}
