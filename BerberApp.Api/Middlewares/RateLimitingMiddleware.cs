using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace BerberApp.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    // Limitler
    private const int MaxRequestsPerHour = 10;  // IP başına saatte max istek
    private const int MaxBookingsPerDay = 3;   // Telefon başına günde max randevu

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Sadece booking endpoint'ini kontrol et
        if (context.Request.Path.StartsWithSegments("/api/booking") &&
            context.Request.Method == "POST")
        {
            var ip = GetClientIp(context);

            // IP rate limit — saatte 10 istek
            var ipKey = $"ratelimit:ip:{ip}:{DateTime.UtcNow:yyyyMMddHH}";
            var ipCount = _cache.GetOrCreate(ipKey, entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddHours(1);
                return 0;
            });

            if (ipCount >= MaxRequestsPerHour)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Çok fazla istek gönderdiniz. Lütfen bekleyin.\"}");
                return;
            }

            _cache.Set(ipKey, ipCount + 1, DateTimeOffset.UtcNow.AddHours(1));
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        return context.Request.Headers["X-Forwarded-For"].FirstOrDefault()
            ?? context.Connection.RemoteIpAddress?.ToString()
            ?? "unknown";
    }
}