using BerberApp.Application.Common.Interfaces;

namespace BerberApp.Api.Middleware;

public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceScopeFactory _scopeFactory;

    public SecurityAuditMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
    {
        _next         = next;
        _scopeFactory = scopeFactory;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var statusCode = context.Response.StatusCode;
        if (statusCode is not (401 or 403 or 429)) return;

        var path = context.Request.Path.Value ?? "-";

        // LOGIN_FAILED zaten AuthController'da loglanıyor; middleware'de çift kayıt olmasın
        if (statusCode == 401 && path.StartsWith("/api/auth/login", StringComparison.OrdinalIgnoreCase))
            return;

        var ip        = GetClientIp(context);
        var method    = context.Request.Method;
        var userAgent = context.Request.Headers.UserAgent.ToString();
        var userId    = context.User?.FindFirst("sub")?.Value
                     ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var (eventType, severity) = statusCode switch
        {
            401 => ("UNAUTHORIZED",  "Warning"),
            403 => ("FORBIDDEN",     "Warning"),
            429 => ("RATE_LIMITED",  "Critical"),
            _   => ("SECURITY_EVENT","Warning"),
        };

        try
        {
            using var scope   = _scopeFactory.CreateScope();
            var auditService  = scope.ServiceProvider.GetRequiredService<IAuditLogService>();
            await auditService.LogAsync(eventType, severity, ip, path, method,
                userId: userId, userAgent: userAgent);
        }
        catch
        {
            // Loglama hatası asıl isteği etkilemesin
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp)) return realIp;
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
