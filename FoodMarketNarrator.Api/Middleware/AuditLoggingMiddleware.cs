using food_market_narrator_api.Models;
using food_market_narrator_api.Services;

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

    public async Task InvokeAsync(HttpContext context, AuditLogService auditLogService)
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

            // Try read body for details
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

            await auditLogService.WriteLogAsync(auditLog);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write audit log for {Path}", context.Request.Path);
        }
    }

    private static (string Action, string TargetType, string? TargetId) MapRequestToAuditAction(
        string method, string path, string query)
    {
        var cleanPath = (path ?? string.Empty).Split('?', 2)[0].Trim('/');
        if (string.IsNullOrWhiteSpace(cleanPath))
        {
            return ("UNKNOWN", "Unknown", null);
        }

        var segments = cleanPath
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (segments.Length == 0)
        {
            return ("UNKNOWN", "Unknown", null);
        }

        // Support both /api/users/... and /Restaurant/... style routes.
        var offset = string.Equals(segments[0], "api", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        if (segments.Length <= offset)
        {
            return ("UNKNOWN", "Unknown", null);
        }

        var root = segments[offset].ToLowerInvariant();
        var id = segments.Length > offset + 1 ? segments[offset + 1] : null;
        var sub = segments.Length > offset + 2 ? segments[offset + 2].ToLowerInvariant() : null;
        var nested = segments.Length > offset + 3 ? segments[offset + 3].ToLowerInvariant() : null;
        var methodUpper = method.ToUpperInvariant();

        // Fine-grained saler/admin actions for better observability in Admin Logs.
        if (root == "restaurant")
        {
            if (methodUpper == "POST" && string.IsNullOrWhiteSpace(id))
                return ("RESTAURANT_CREATE", "Restaurant", null);

            if (!string.IsNullOrWhiteSpace(id) && sub == "status" && methodUpper == "PATCH")
                return ("RESTAURANT_UPDATE_STATUS", "Restaurant", id);

            if (!string.IsNullOrWhiteSpace(id) && sub == "dishes" && methodUpper == "POST")
                return ("DISH_CREATE", "Dish", id);

            if (!string.IsNullOrWhiteSpace(id) && sub == "images" && methodUpper == "POST")
                return ("IMAGE_UPLOAD", "Image", id);

            if (!string.IsNullOrWhiteSpace(id) && sub == "images" && nested == "reorder" && methodUpper == "PATCH")
                return ("IMAGE_REORDER", "Image", id);

            if (!string.IsNullOrWhiteSpace(id) && sub == "audios" && methodUpper == "POST")
                return ("AUDIO_UPLOAD", "Audio", id);

            if (!string.IsNullOrWhiteSpace(id) && methodUpper == "PATCH")
                return ("RESTAURANT_UPDATE", "Restaurant", id);
        }

        if (root == "dishes" && !string.IsNullOrWhiteSpace(id))
        {
            return methodUpper switch
            {
                "PUT" => ("DISH_UPDATE", "Dish", id),
                "DELETE" => ("DISH_DELETE", "Dish", id),
                _ => ("UPDATE", "Dish", id)
            };
        }

        if (root == "images" && !string.IsNullOrWhiteSpace(id))
        {
            if (methodUpper == "PATCH" && sub == "primary")
                return ("IMAGE_SET_PRIMARY", "Image", id);

            return methodUpper switch
            {
                "PUT" => ("IMAGE_REPLACE", "Image", id),
                "DELETE" => ("IMAGE_DELETE", "Image", id),
                _ => ("UPDATE", "Image", id)
            };
        }

        if (root == "audios" && !string.IsNullOrWhiteSpace(id))
        {
            if (methodUpper == "PATCH" && sub == "active")
                return ("AUDIO_SET_ACTIVE", "Audio", id);

            if (methodUpper == "DELETE")
                return ("AUDIO_DELETE", "Audio", id);
        }

        if (root == "users")
        {
            if (methodUpper == "POST")
                return ("USER_CREATE", "User", null);

            if (!string.IsNullOrWhiteSpace(id) && methodUpper == "PATCH" && sub == "role")
                return ("USER_UPDATE_ROLE", "User", id);

            if (!string.IsNullOrWhiteSpace(id) && methodUpper == "PATCH" && sub == "status")
                return ("USER_UPDATE_STATUS", "User", id);

            if (!string.IsNullOrWhiteSpace(id) && methodUpper == "DELETE")
                return ("USER_DELETE", "User", id);
        }

        var targetType = ToTargetType(root);

        return methodUpper switch
        {
            "POST" => ("CREATE", targetType, null),
            "PUT" => ("UPDATE", targetType, id),
            "PATCH" => sub switch
            {
                "status" => ("UPDATE_STATUS", targetType, id),
                "role" => ("UPDATE_ROLE", targetType, id),
                _ => ("UPDATE", targetType, id)
            },
            "DELETE" => ("DELETE", targetType, id),
            _ => ("UNKNOWN", targetType, id)
        };
    }

    private static string ToTargetType(string root)
    {
        return root.ToLowerInvariant() switch
        {
            "restaurant" => "Restaurant",
            "dishes" => "Dish",
            "images" => "Image",
            "audios" => "Audio",
            "users" => "User",
            "auth" => "Auth",
            _ => string.IsNullOrWhiteSpace(root)
                ? "Unknown"
                : char.ToUpperInvariant(root[0]) + root[1..]
        };
    }
}
