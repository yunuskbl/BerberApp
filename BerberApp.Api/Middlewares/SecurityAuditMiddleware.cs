namespace BerberApp.Api.Middleware;

/// <summary>
/// Güvenlik denetim logu: 401/403 yanıtlarını ve şüpheli aktiviteleri kayıt altına alır.
/// Middleware sırasında en dışta yer almalı (Program.cs'de ilk UseMiddleware).
/// </summary>
public class SecurityAuditMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SecurityAuditMiddleware> _logger;

    public SecurityAuditMiddleware(RequestDelegate next, ILogger<SecurityAuditMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);

        var statusCode = context.Response.StatusCode;

        // Sadece güvenlikle ilgili durum kodlarını logla
        if (statusCode is 401 or 403 or 429)
        {
            var ip        = GetClientIp(context);
            var path      = context.Request.Path.Value ?? "-";
            var method    = context.Request.Method;
            var userAgent = context.Request.Headers.UserAgent.ToString();
            if (userAgent.Length > 120) userAgent = userAgent[..120] + "…";

            // JWT'den kullanıcı ID'sini al (eğer token decode edilebildiyse)
            var userId = context.User?.FindFirst("sub")?.Value
                      ?? context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? "-";

            var eventType = statusCode switch
            {
                401 => "UNAUTHORIZED",
                403 => "FORBIDDEN",
                429 => "RATE_LIMITED",
                _   => "SECURITY_EVENT"
            };

            _logger.LogWarning(
                "[SECURITY:{EventType}] Status={Status} Path={Path} Method={Method} IP={Ip} UserId={UserId} UA={UserAgent}",
                eventType, statusCode, path, method, ip, userId, userAgent);
        }
    }

    private static string GetClientIp(HttpContext context)
    {
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp)) return realIp;
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}
