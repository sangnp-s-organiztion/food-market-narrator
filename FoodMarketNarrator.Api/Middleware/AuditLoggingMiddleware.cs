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
        // Skip non-authenticated, GET, static files, swagger
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

        // Read body BEFORE _next so the stream hasn't been consumed yet
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
