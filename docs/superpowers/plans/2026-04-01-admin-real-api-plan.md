# Admin Real API Integration — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Chuyển toàn bộ admin frontend từ mock data sang dùng API thực. Backend cần thêm AuditLog entity, middleware tự ghi log, và 3 API endpoint mới. Frontend cần cập nhật Dashboard và Logs page.

**Architecture:**
- Backend: thêm AuditLog model (SQL Server via EF Core) + middleware tự động ghi log + 3 endpoint mới (audit-logs, entity-counts, listens-timeseries). AnalyticsService mở rộng thêm 2 method.
- Frontend: thêm `auditApi.ts`, cập nhật `analyticsApi.ts`, sửa `Index.tsx` + `LogsPage.tsx`, xóa `mockData.ts`.
- Không sửa mobile app, saler frontend, hay API contract đã có.

**Tech Stack:** ASP.NET Core Web API (.NET 10), Entity Framework Core (SQL Server), React + TypeScript + TanStack Query, MongoDB

---

## File Map

### Backend — Tạo mới
- `FoodMarketNarrator.Api/Models/AuditLog.cs` — entity
- `FoodMarketNarrator.Api/DTOs/AuditLog/AuditLogResponse.cs` — response DTO
- `FoodMarketNarrator.Api/DTOs/Analytics/EntityCountsResponse.cs` — entity counts DTO
- `FoodMarketNarrator.Api/DTOs/Analytics/ListensTimeseriesResponse.cs` — timeseries DTO
- `FoodMarketNarrator.Api/Middleware/AuditLoggingMiddleware.cs` — tự động ghi log
- `FoodMarketNarrator.Api/Controllers/AuditLogsController.cs` — audit-logs CRUD

### Backend — Sửa
- `FoodMarketNarrator.Api/Data/Context/AppDbContext.cs` — thêm DbSet<AuditLog>
- `FoodMarketNarrator.Api/Program.cs` — đăng ký middleware + AuditLogRepository + AuditLogService
- `FoodMarketNarrator.Api/Services/AnalyticsService.cs` — thêm GetEntityCountsAsync + GetListensTimeseriesAsync
- `FoodMarketNarrator.Api/Repositories/AnalyticsRepository.cs` — thêm GetDailyListenCountsAsync (MongoDB)
- `FoodMarketNarrator.Api/Controllers/AnalyticsController.cs` — thêm 2 endpoint
- `FoodMarketNarrator.Api/Migrations/` — tạo migration mới cho AuditLog

### Frontend Admin — Tạo mới
- `admin/src/lib/auditApi.ts` — gọi GET /api/audit-logs

### Frontend Admin — Sửa
- `admin/src/types/analytics.ts` — thêm EntityCountsResponse, ListensTimeseriesResponse, AuditLogResponse types
- `admin/src/lib/analyticsApi.ts` — thêm getEntityCounts(), getListensTimeseries(days)
- `admin/src/pages/Index.tsx` — thay entity stats + daily listens chart mock bằng API
- `admin/src/pages/LogsPage.tsx` — thay recent-activity bằng audit-logs API, thêm pagination + filter
- `admin/src/components/HeatmapSection.tsx` — xóa mockData fallback

### Frontend Admin — Xóa
- `admin/src/lib/mockData.ts`
- `admin/src/lib/adminLogs.ts`

---

## BACKEND TASKS

### Task 1: Tạo AuditLog entity và migration

**Files:**
- Create: `FoodMarketNarrator.Api/Models/AuditLog.cs`
- Create: `FoodMarketNarrator.Api/DTOs/AuditLog/AuditLogResponse.cs`
- Modify: `FoodMarketNarrator.Api/Data/Context/AppDbContext.cs:1-20`

- [ ] **Step 1: Tạo AuditLog.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace food_market_narrator_api.Models;

[Table("AuditLogs")]
public class AuditLog
{
    [Key]
    [Column("id")]
    public int Id { get; set; }

    [Required]
    [Column("user_id")]
    public int UserId { get; set; }

    [Required]
    [Column("username")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column("action")]
    [MaxLength(20)]
    public string Action { get; set; } = string.Empty; // LOCK, UNLOCK, CREATE, UPDATE, DELETE, LOGIN, LOGOUT

    [Required]
    [Column("target_type")]
    [MaxLength(50)]
    public string TargetType { get; set; } = string.Empty; // Restaurant, User, Audio, Dish, Image

    [Column("target_id")]
    [MaxLength(100)]
    public string? TargetId { get; set; }

    [Column("target_name")]
    [MaxLength(255)]
    public string? TargetName { get; set; }

    [Column("details")]
    public string? Details { get; set; } // JSON extra info

    [Column("ip_address")]
    [MaxLength(45)]
    public string? IpAddress { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

- [ ] **Step 2: Tạo AuditLogResponse DTO**

```csharp
namespace food_market_narrator_api.DTOs.AuditLog;

public class AuditLogResponse
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public string? TargetId { get; set; }
    public string? TargetName { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

- [ ] **Step 3: Thêm DbSet vào AppDbContext.cs**

Thêm vào class `AppDbContext`:
```csharp
public DbSet<AuditLog> AuditLogs { get; set; }
```

- [ ] **Step 4: Tạo EF Core migration**

Run: `cd FoodMarketNarrator.Api && dotnet ef migrations add AddAuditLogs --output-dir Migrations`
Expected: Migration file được tạo trong `Migrations/`

- [ ] **Step 5: Commit**

```bash
git add FoodMarketNarrator.Api/Models/AuditLog.cs FoodMarketNarrator.Api/DTOs/AuditLog/ FoodMarketNarrator.Api/Data/Context/AppDbContext.cs FoodMarketNarrator.Api/Migrations/
git commit -m "feat(api): add AuditLog entity and EF migration"
```

---

### Task 2: Tạo AuditLoggingMiddleware

**Files:**
- Create: `FoodMarketNarrator.Api/Middleware/AuditLoggingMiddleware.cs`

- [ ] **Step 1: Viết middleware**

```csharp
using System.Text.Json;
using System.Text.RegularExpressions;
using food_market_narrator_api.Data.Context;
using food_market_narrator_api.Models;

namespace food_market_narrator_api.Middleware;

public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db)
    {
        // Skip non-authenticated, GET, static files, health checks
        if (context.User.Identity?.IsAuthenticated != true
            || string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase)
            || context.Request.Path.StartsWithSegments("/swagger")
            || context.Request.Path.StartsWithSegments("/maui-images")
            || context.Request.Path.StartsWithSegments("/maui-audios")
            || context.Request.Path.StartsWithSegments("/uploads"))
        {
            await _next(context);
            return;
        }

        // Skip login/logout — handled by AuthController explicitly
        if (context.Request.Path.StartsWithSegments("/Auth/login")
            || context.Request.Path.StartsWithSegments("/Auth/logout"))
        {
            await _next(context);
            return;
        }

        await _next(context);

        // Only log on successful responses (2xx)
        if (context.Response.StatusCode < 200 || context.Response.StatusCode >= 300)
            return;

        try
        {
            var userIdClaim = context.User.FindFirst("user_id")?.Value
                              ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var username = context.User.Identity?.Name
                          ?? context.User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value
                          ?? "unknown";

            if (!int.TryParse(userIdClaim, out var userId))
                userId = 0;

            var (action, targetType, targetId) = MapRequestToAuditAction(
                context.Request.Method,
                context.Request.Path.Value ?? "",
                context.Request.QueryString.Value ?? ""
            );

            // Try read body for status change (isActive field)
            string? details = null;
            if (context.Request.ContentLength > 0 && context.Request.ContentType == "application/json")
            {
                context.Request.EnableBuffering();
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                if (!string.IsNullOrWhiteSpace(body))
                    details = body.Length > 500 ? body[..500] : body;
            }

            var auditLog = new AuditLog
            {
                UserId = userId,
                Username = username,
                Action = action,
                TargetType = targetType,
                TargetId = targetId,
                Details = details,
                IpAddress = context.Connection.RemoteIpAddress?.ToString(),
                CreatedAt = DateTime.UtcNow
            };

            db.AuditLogs.Add(auditLog);
            await db.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for {Path}", context.Request.Path);
        }
    }

    private static (string Action, string TargetType, string? TargetId) MapRequestToAuditAction(
        string method, string path, string query)
    {
        // Route patterns: /api/Restaurant/{id}/status, /api/Users/{id}/role, etc.
        // Pattern: /api/Users/5/role  → Users, 5
        // Pattern: /api/Restaurant/abc123/status → Restaurant, abc123

        var routeMatch = Regex.Match(path, @"^/api/(\w+)/([^/]+)(?:/(\w+))?", RegexOptions.IgnoreCase);
        string targetType = routeMatch.Success ? routeMatch.Groups[1].Value : "Unknown";
        string? targetId = routeMatch.Success && routeMatch.Groups[2].Success
            ? routeMatch.Groups[2].Value : null;
        string subAction = routeMatch.Success && routeMatch.Groups[3].Success
            ? routeMatch.Groups[3].Value : "";

        return method.ToUpperInvariant() switch
        {
            "POST" => ("CREATE", Capitalize(targetType), null),
            "PUT" => ("UPDATE", Capitalize(targetType), targetId),
            "PATCH" => subAction.ToLowerInvariant() switch
            {
                "status" => ("UPDATE_STATUS", Capitalize(targetType), targetId),
                "role" => ("UPDATE_ROLE", Capitalize(targetType), targetId),
                _ => ("UPDATE", Capitalize(targetType), targetId)
            },
            "DELETE" => ("DELETE", Capitalize(targetType), targetId),
            _ => ("UNKNOWN", Capitalize(targetType), targetId)
        };
    }

    private static string Capitalize(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        return char.ToUpperInvariant(s[0]) + s[1..];
    }
}
```

- [ ] **Step 2: Đăng ký middleware trong Program.cs**

Thêm sau `app.UseAuthorization();` và trước `app.MapControllers();`:
```csharp
app.UseMiddleware<food_market_narrator_api.Middleware.AuditLoggingMiddleware>();
```

- [ ] **Step 3: Commit**

```bash
git add FoodMarketNarrator.Api/Middleware/AuditLoggingMiddleware.cs FoodMarketNarrator.Api/Program.cs
git commit -m "feat(api): add AuditLoggingMiddleware for automatic admin action tracking"
```

---

### Task 3: Tạo AuditLogsController + AuditLogService

**Files:**
- Create: `FoodMarketNarrator.Api/Services/AuditLogService.cs`
- Create: `FoodMarketNarrator.Api/Controllers/AuditLogsController.cs`

- [ ] **Step 1: Tạo AuditLogService**

```csharp
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

    public async Task WriteLogAsync(AuditLog log)
    {
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();
    }
}
```

- [ ] **Step 2: Tạo AuditLogsController**

```csharp
using food_market_narrator_api.DTOs.AuditLog;
using food_market_narrator_api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace food_market_narrator_api.Controllers;

[ApiController]
[Route("api/audit-logs")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly AuditLogService _auditLogService;

    public AuditLogsController(AuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] int? userId = null,
        [FromQuery] string? action = null,
        [FromQuery] string? targetType = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        page = Math.Clamp(page, 1, 1000);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var (items, totalCount) = await _auditLogService.GetLogsAsync(
            page, pageSize, userId, action, targetType, from, to);

        return Ok(new
        {
            items,
            totalCount,
            page,
            pageSize
        });
    }
}
```

- [ ] **Step 3: Đăng ký AuditLogService trong Program.cs**

Thêm vào DI container (sau các `builder.Services.AddScoped` khác):
```csharp
builder.Services.AddScoped<AuditLogService>();
```

- [ ] **Step 4: Build để kiểm tra**

Run: `cd FoodMarketNarrator.Api && dotnet build`
Expected: Build thành công không lỗi

- [ ] **Step 5: Commit**

```bash
git add FoodMarketNarrator.Api/Services/AuditLogService.cs FoodMarketNarrator.Api/Controllers/AuditLogsController.cs FoodMarketNarrator.Api/Program.cs
git commit -m "feat(api): add AuditLogsController and AuditLogService"
```

---

### Task 4: Thêm entity-counts và listens-timeseries endpoint

**Files:**
- Create: `FoodMarketNarrator.Api/DTOs/Analytics/EntityCountsResponse.cs`
- Create: `FoodMarketNarrator.Api/DTOs/Analytics/ListensTimeseriesResponse.cs`
- Modify: `FoodMarketNarrator.Api/Repositories/AnalyticsRepository.cs`
- Modify: `FoodMarketNarrator.Api/Services/AnalyticsService.cs`
- Modify: `FoodMarketNarrator.Api/Controllers/AnalyticsController.cs`

- [ ] **Step 1: Tạo EntityCountsResponse.cs**

```csharp
namespace food_market_narrator_api.DTOs.Analytics;

public class EntityCountsResponse
{
    public int TotalRestaurants { get; set; }
    public int TotalAudios { get; set; }
    public int TotalUsers { get; set; }
    public int TotalDishes { get; set; }
}
```

- [ ] **Step 2: Tạo ListensTimeseriesResponse.cs**

```csharp
namespace food_market_narrator_api.DTOs.Analytics;

public class ListensTimeseriesResponse
{
    public List<ListenCountItem> Items { get; set; } = [];
}

public class ListenCountItem
{
    public string Date { get; set; } = string.Empty; // "yyyy-MM-dd"
    public int Listens { get; set; }
}
```

- [ ] **Step 3: Thêm method vào AnalyticsRepository.cs**

Thêm vào cuối class (trước `}` đóng):

```csharp
// ─── Daily listen counts for timeseries (group by date, valid plays only) ─
public async Task<List<DailyListenCount>> GetDailyListenCountsAsync(int days = 14)
{
    var since = DateTime.UtcNow.AddDays(-days);

    var pipeline = new[]
    {
        new BsonDocument("$match",
            new BsonDocument
            {
                { "timestamp", new BsonDocument("$gte", since)),
                { "duration", new BsonDocument("$gte", 5))
            }),
        new BsonDocument("$group",
            new BsonDocument
            {
                { "_id", new BsonDocument("$dateToString",
                    new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$timestamp" } }) },
                { "count", new BsonDocument("$sum", 1) }
            }),
        new BsonDocument("$sort",
            new BsonDocument("_id", 1))
    };

    var results = await _db.GetCollection<BsonDocument>("AudioLogs")
        .Aggregate<BsonDocument>(pipeline)
        .ToListAsync();

    return results.Select(r => new DailyListenCount
    {
        Date = r["_id"].ToString(),
        Count = r["count"].ToInt32()
    }).ToList();
}

public class DailyListenCount
{
    public string Date { get; set; } = string.Empty;
    public int Count { get; set; }
}
```

- [ ] **Step 4: Thêm 2 method vào AnalyticsService.cs**

Thêm vào class `AnalyticsService` (sau `GetAllAudioStatsAsync`):

```csharp
public async Task<EntityCountsResponse> GetEntityCountsAsync()
{
    var totalRestaurants = await _dbContext.Restaurant.CountAsync();
    var totalAudios = await _dbContext.Audio.CountAsync();
    var totalUsers = await _dbContext.User.CountAsync();
    var totalDishes = await _dbContext.Dish.CountAsync();

    return new EntityCountsResponse
    {
        TotalRestaurants = totalRestaurants,
        TotalAudios = totalAudios,
        TotalUsers = totalUsers,
        TotalDishes = totalDishes
    };
}

public async Task<ListensTimeseriesResponse> GetListensTimeseriesAsync(int days = 14)
{
    var clampedDays = Math.Clamp(days, 1, 90);
    var dailyCounts = await _analyticsRepository.GetDailyListenCountsAsync(clampedDays);

    // Fill gaps: if a date has no listens, include it with count=0
    var result = new List<ListenCountItem>();
    var today = DateTime.UtcNow.Date;
    for (int i = clampedDays - 1; i >= 0; i--)
    {
        var date = today.AddDays(-i);
        var dateStr = date.ToString("yyyy-MM-dd");
        var found = dailyCounts.FirstOrDefault(d => d.Date == dateStr);
        result.Add(new ListenCountItem
        {
            Date = dateStr,
            Listens = found?.Count ?? 0
        });
    }

    return new ListensTimeseriesResponse { Items = result };
}
```

Import thêm ở đầu file:
```csharp
using food_market_narrator_api.DTOs.Analytics;
```

- [ ] **Step 5: Thêm 2 endpoint vào AnalyticsController.cs**

Thêm sau `GetRecentActivity`:

```csharp
[HttpGet("entity-counts")]
public async Task<IActionResult> GetEntityCounts()
{
    var result = await _analyticsService.GetEntityCountsAsync();
    return Ok(result);
}

[HttpGet("listens-timeseries")]
public async Task<IActionResult> GetListensTimeseries([FromQuery] int days = 14)
{
    var result = await _analyticsService.GetListensTimeseriesAsync(days);
    return Ok(result);
}
```

- [ ] **Step 6: Build**

Run: `cd FoodMarketNarrator.Api && dotnet build`
Expected: Build thành công

- [ ] **Step 7: Commit**

```bash
git add FoodMarketNarrator.Api/DTOs/Analytics/EntityCountsResponse.cs FoodMarketNarrator.Api/DTOs/Analytics/ListensTimeseriesResponse.cs FoodMarketNarrator.Api/Repositories/AnalyticsRepository.cs FoodMarketNarrator.Api/Services/AnalyticsService.cs FoodMarketNarrator.Api/Controllers/AnalyticsController.cs
git commit -m "feat(api): add entity-counts and listens-timeseries analytics endpoints"
```

---

### Task 5: Update AuthController để ghi LOGIN/LOGOUT audit log

**Files:**
- Modify: `FoodMarketNarrator.Api/Controllers/AuthController.cs`

- [ ] **Step 1: Đọc AuthController.cs hiện tại**

Xem nội dung AuthController để biết cách login/logout được xử lý.

- [ ] **Step 2: Thêm audit log cho LOGIN và LOGOUT**

Trong AuthController, inject `AuditLogService` và gọi `WriteLogAsync`:
- Sau khi login thành công: ghi action="LOGIN", targetType="User"
- Khi logout: ghi action="LOGOUT", targetType="User"

Example pattern (điều chỉnh theo code thực tế):
```csharp
// Trong Login action, sau khi SetAuthenticated:
await _auditLogService.WriteLogAsync(new AuditLog
{
    UserId = user.UserId,
    Username = user.Username,
    Action = "LOGIN",
    TargetType = "User",
    TargetId = user.UserId.ToString(),
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
    CreatedAt = DateTime.UtcNow
});

// Trong Logout action:
var userIdClaim = User.FindFirst("user_id")?.Value;
var username = User.Identity?.Name;
if (int.TryParse(userIdClaim, out var uid))
{
    await _auditLogService.WriteLogAsync(new AuditLog
    {
        UserId = uid,
        Username = username ?? "unknown",
        Action = "LOGOUT",
        TargetType = "User",
        IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
        CreatedAt = DateTime.UtcNow
    });
}
```

- [ ] **Step 3: Build + commit**

Run: `cd FoodMarketNarrator.Api && dotnet build`
Commit: `git add ... && git commit -m "feat(api): log LOGIN/LOGOUT actions in AuthController"`

---

## FRONTEND ADMIN TASKS

### Task 6: Thêm TypeScript types và API client

**Files:**
- Modify: `admin/src/types/analytics.ts`
- Create: `admin/src/lib/auditApi.ts`
- Modify: `admin/src/lib/analyticsApi.ts`

- [ ] **Step 1: Thêm types vào analytics.ts**

Thêm vào cuối file:

```typescript
export interface EntityCounts {
  totalRestaurants: number;
  totalAudios: number;
  totalUsers: number;
  totalDishes: number;
}

export interface ListenCountItem {
  date: string; // "yyyy-MM-dd"
  listens: number;
}

export interface ListensTimeseries {
  items: ListenCountItem[];
}

export interface AuditLogItem {
  id: number;
  userId: number;
  username: string;
  action: string;
  targetType: string;
  targetId: string | null;
  targetName: string | null;
  details: string | null;
  ipAddress: string | null;
  createdAt: string;
}

export interface AuditLogsResponse {
  items: AuditLogItem[];
  totalCount: number;
  page: number;
  pageSize: number;
}
```

- [ ] **Step 2: Tạo auditApi.ts**

```typescript
import type { AuditLogsResponse } from "@/types/analytics";

const API_BASE = import.meta.env.VITE_API_BASE_URL ?? "http://localhost:5044";

async function auditFetch<T>(path: string): Promise<T> {
  const res = await fetch(`${API_BASE}${path}`, {
    credentials: "include",
  });
  if (!res.ok) {
    throw new Error(`Audit API error ${res.status}: ${res.statusText}`);
  }
  return res.json() as Promise<T>;
}

export interface AuditLogFilters {
  page?: number;
  pageSize?: number;
  userId?: number;
  action?: string;
  targetType?: string;
  from?: string;
  to?: string;
}

function buildQuery(filters: AuditLogFilters): string {
  const params = new URLSearchParams();
  if (filters.page) params.set("page", String(filters.page));
  if (filters.pageSize) params.set("pageSize", String(filters.pageSize));
  if (filters.userId) params.set("userId", String(filters.userId));
  if (filters.action) params.set("action", filters.action);
  if (filters.targetType) params.set("targetType", filters.targetType);
  if (filters.from) params.set("from", filters.from);
  if (filters.to) params.set("to", filters.to);
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

export const auditApi = {
  getLogs(filters: AuditLogFilters = {}): Promise<AuditLogsResponse> {
    return auditFetch<AuditLogsResponse>(`/api/audit-logs${buildQuery(filters)}`);
  },
};
```

- [ ] **Step 3: Thêm methods vào analyticsApi.ts**

Thêm vào cuối object `analyticsApi`:

```typescript
async getEntityCounts(): Promise<EntityCounts> {
  return analyticsFetch<EntityCounts>("/api/analytics/entity-counts");
},

async getListensTimeseries(days = 14): Promise<ListensTimeseries> {
  return analyticsFetch<ListensTimeseries>(
    `/api/analytics/listens-timeseries?days=${days}`
  );
},
```

Import thêm:
```typescript
import type {
  EntityCounts,
  ListensTimeseries,
  // ...existing imports keep same
} from "@/types/analytics";
```

- [ ] **Step 4: Commit**

```bash
git add admin/src/types/analytics.ts admin/src/lib/auditApi.ts admin/src/lib/analyticsApi.ts
git commit -m "feat(admin): add AuditLog types, auditApi client, and analyticsApi extensions"
```

---

### Task 7: Cập nhật Dashboard Index.tsx

**Files:**
- Modify: `admin/src/pages/Index.tsx`

- [ ] **Step 1: Thay entityStats mock bằng API query**

Tìm phần `entityStats` array trong `Index.tsx` và thay thế:

```typescript
// XÓA: import { restaurants } from "@/lib/mockData";
// XÓA: const entityStats = [ ... hardcoded ... ];

// THAY BẰNG:
const { data: entityCounts } = useQuery({
  queryKey: ["analytics", "entity-counts"],
  queryFn: () => analyticsApi.getEntityCounts(),
  staleTime: 60_000,
});

const entityStats = [
  {
    label: "Tổng nhà hàng",
    value: entityCounts?.totalRestaurants ?? "—",
    delta: null,
    deltaType: "neutral" as const,
    icon: Store,
  },
  {
    label: "Tổng âm thanh",
    value: entityCounts?.totalAudios ?? "—",
    delta: null,
    deltaType: "neutral" as const,
    icon: Headphones,
  },
  {
    label: "Người dùng",
    value: entityCounts?.totalUsers ?? "—",
    delta: null,
    deltaType: "neutral" as const,
    icon: Users,
  },
  {
    label: "Tổng món ăn",
    value: entityCounts?.totalDishes ?? "—",
    delta: null,
    deltaType: "neutral" as const,
    icon: UtensilsCrossed,
  },
];
```

Lưu ý: `value` giờ là `number | "—"`, component stat-card cần xử lý — nếu `value` là string thì render trực tiếp.

- [ ] **Step 2: Thay daily listens chart mock bằng API query**

```typescript
const { data: timeseriesData } = useQuery({
  queryKey: ["analytics", "listens-timeseries", 14],
  queryFn: () => analyticsApi.getListensTimeseries(14),
  staleTime: 60_000,
});

// Chart data: convert API format to Recharts format
const dailyListensData = useMemo(() => {
  return (timeseriesData?.items ?? []).map((item) => ({
    date: item.date.slice(5), // "03-01" from "2026-03-01"
    listens: item.listens,
  }));
}, [timeseriesData]);
```

Thay `<AreaChart data={[...]}` bằng `<AreaChart data={dailyListensData}>`.

- [ ] **Step 3: Commit**

```bash
git add admin/src/pages/Index.tsx
git commit -m "feat(admin): replace mock entity stats and daily listens chart with real APIs"
```

---

### Task 8: Cập nhật LogsPage.tsx — dùng audit-logs API

**Files:**
- Modify: `admin/src/pages/LogsPage.tsx`

- [ ] **Step 1: Viết lại LogsPage**

Thay toàn bộ nội dung `LogsPage.tsx` bằng implementation mới sử dụng `auditApi.getLogs()`. Key changes:

```tsx
import { useState } from "react";
import { useQuery } from "@tanstack/react-query";
import { auditApi } from "@/lib/auditApi";
import type { AuditLogItem } from "@/types/analytics";

const ACTION_LABELS: Record<string, { label: string; cls: string }> = {
  LOGIN:        { label: "Đăng nhập",    cls: "bg-emerald-100 text-emerald-700" },
  LOGOUT:       { label: "Đăng xuất",   cls: "bg-slate-100 text-slate-700" },
  CREATE:       { label: "Tạo mới",     cls: "bg-blue-100 text-blue-700" },
  UPDATE:       { label: "Cập nhật",    cls: "bg-amber-100 text-amber-700" },
  UPDATE_STATUS:{ label: "Đổi trạng thái", cls: "bg-amber-100 text-amber-700" },
  UPDATE_ROLE: { label: "Đổi quyền",   cls: "bg-purple-100 text-purple-700" },
  DELETE:      { label: "Xóa",          cls: "bg-red-100 text-red-700" },
};

function formatTimestamp(iso: string): string {
  try {
    return new Date(iso).toLocaleString("vi-VN", {
      day: "2-digit", month: "2-digit", year: "numeric",
      hour: "2-digit", minute: "2-digit",
    });
  } catch { return iso; }
}

export default function LogsPage() {
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const { data, isLoading, isError } = useQuery({
    queryKey: ["audit-logs", page],
    queryFn: () => auditApi.getLogs({ page, pageSize: PAGE_SIZE }),
    staleTime: 30_000,
    refetchInterval: 30_000,
  });

  const totalPages = data ? Math.ceil(data.totalCount / PAGE_SIZE) : 0;

  return (
    <AdminLayout>
      <div className="page-header">
        <h1 className="page-title">Nhật ký hành động Admin</h1>
        <span className="text-xs text-muted-foreground mono">
          Tự động cập nhật mỗi 30 giây
        </span>
      </div>

      <div className="max-w-7xl mx-auto px-8 py-6">
        <div className="stat-card">
          {isLoading && <p className="text-center py-8 text-muted-foreground">Đang tải…</p>}
          {isError && <p className="text-center py-8 text-destructive">Không thể tải nhật ký.</p>}

          {!isLoading && !isError && data && (
            <>
              <table className="data-table">
                <thead>
                  <tr>
                    <th>Thời gian</th>
                    <th>Người dùng</th>
                    <th>Hành động</th>
                    <th>Đối tượng</th>
                    <th>Chi tiết</th>
                  </tr>
                </thead>
                <tbody>
                  {data.items.length === 0 && (
                    <tr>
                      <td colSpan={5} className="text-center py-8 text-muted-foreground">
                        Chưa có nhật ký nào.
                      </td>
                    </tr>
                  )}
                  {data.items.map((log: AuditLogItem) => {
                    const actionInfo = ACTION_LABELS[log.action] ?? {
                      label: log.action,
                      cls: "bg-gray-100 text-gray-700",
                    };
                    return (
                      <tr key={log.id}>
                        <td className="mono text-xs whitespace-nowrap">
                          {formatTimestamp(log.createdAt)}
                        </td>
                        <td className="text-sm">{log.username}</td>
                        <td>
                          <span className={cn("inline-block px-2 py-0.5 rounded-full text-xs font-medium", actionInfo.cls)}>
                            {actionInfo.label}
                          </span>
                        </td>
                        <td className="text-sm">
                          {log.targetType}
                          {log.targetId ? ` #${log.targetId}` : ""}
                          {log.targetName ? ` — ${log.targetName}` : ""}
                        </td>
                        <td className="text-xs text-muted-foreground max-w-[200px] truncate">
                          {log.details ?? "—"}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>

              {/* Pagination */}
              {totalPages > 1 && (
                <div className="flex items-center justify-between mt-4 px-1">
                  <span className="text-xs text-muted-foreground">
                    Trang {page} / {totalPages} — {data.totalCount} bản ghi
                  </span>
                  <div className="flex gap-2">
                    <button
                      className="px-3 py-1 text-sm border rounded disabled:opacity-50"
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                      disabled={page <= 1}
                    >
                      ← Trước
                    </button>
                    <button
                      className="px-3 py-1 text-sm border rounded disabled:opacity-50"
                      onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                      disabled={page >= totalPages}
                    >
                      Sau →
                    </button>
                  </div>
                </div>
              )}

              <p className="text-xs text-muted-foreground mt-3 px-1">
                Nhật ký tự động cập nhật mỗi 30 giây.
              </p>
            </>
          )}
        </div>
      </div>
    </AdminLayout>
  );
}
```

- [ ] **Step 2: Commit**

```bash
git add admin/src/pages/LogsPage.tsx
git commit -m "feat(admin): rewrite LogsPage to use audit-logs API with pagination"
```

---

### Task 9: Xóa mock data files

**Files:**
- Delete: `admin/src/lib/mockData.ts`
- Delete: `admin/src/lib/adminLogs.ts`
- Modify: `admin/src/components/HeatmapSection.tsx`

- [ ] **Step 1: Kiểm tra HeatmapSection.tsx còn import mockData không**

Đọc file. Nếu còn `import { restaurants as mockRestaurants } from "@/lib/mockData"` → xóa dòng đó và thay fallback bằng empty array:

```typescript
// XÓA: import { restaurants as mockRestaurants } from "@/lib/mockData";

// Trong component, thay:
// const pois = restaurantPois.length > 0 ? restaurantPois : mockRestaurants;
// BẰNG:
// const pois = restaurantPois ?? [];
```

- [ ] **Step 2: Xóa mockData.ts và adminLogs.ts**

```bash
rm admin/src/lib/mockData.ts admin/src/lib/adminLogs.ts
```

- [ ] **Step 3: Verify build frontend**

Run: `cd admin && npm run build`
Expected: Build thành công không lỗi (TypeScript không tìm thấy mockData)

- [ ] **Step 4: Commit**

```bash
git add -A admin/src/
git commit -m "chore(admin): remove mockData.ts and adminLogs.ts, fix HeatmapSection"
```

---

## Final: Apply migration + verify

- [ ] **Run migration trên database**

```bash
cd FoodMarketNarrator.Api
dotnet ef database update
```

- [ ] **Verify API startup**

```bash
cd FoodMarketNarrator.Api
dotnet run
```

Kiểm tra: `GET /api/audit-logs` → 401 (chưa login), sau login → 200 với empty array
Kiểm tra: `GET /api/analytics/entity-counts` → trả về số entity
Kiểm tra: `GET /api/analytics/listens-timeseries` → trả về timeseries

- [ ] **Verify Admin Frontend**

```bash
cd admin
npm run dev
```

Login → Dashboard hiển thị entity counts từ API → Chart hiển thị timeseries data → Logs page hiển thị audit log với pagination → Không còn lỗi TypeScript về mockData
