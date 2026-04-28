using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace BerberApp.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    // Limitler
    private const int MaxBookingRequestsPerHour = 10;  // IP başına saatte max booking isteği
    private const int MaxLoginAttemptsPerWindow = 5;   // IP başına 15 dakikada max login denemesi

    public RateLimitingMiddleware(RequestDelegate next, IMemoryCache cache)
    {
        _next = next;
        _cache = cache;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var ip = GetClientIp(context);

        // Login brute-force koruması — 15 dakikada 5 deneme
        if (context.Request.Path.StartsWithSegments("/api/auth/login") &&
            context.Request.Method == "POST")
        {
            var window = DateTime.UtcNow.ToString("yyyyMMddHHmm")[..11]; // 15 dakikalık pencere
            var loginKey = $"ratelimit:login:{ip}:{window}";
            var loginCount = _cache.GetOrCreate(loginKey, entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(15);
                return 0;
            });

            if (loginCount >= MaxLoginAttemptsPerWindow)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Çok fazla giriş denemesi. 15 dakika bekleyin.\"}");
                return;
            }

            _cache.Set(loginKey, loginCount + 1, DateTimeOffset.UtcNow.AddMinutes(15));
        }

        // Booking rate limit — saatte 10 istek
        if (context.Request.Path.StartsWithSegments("/api/booking") &&
            context.Request.Method == "POST")
        {
            var ipKey = $"ratelimit:ip:{ip}:{DateTime.UtcNow:yyyyMMddHH}";
            var ipCount = _cache.GetOrCreate(ipKey, entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddHours(1);
                return 0;
            });

            if (ipCount >= MaxBookingRequestsPerHour)
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