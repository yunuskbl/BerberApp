using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace BerberApp.Api.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;

    // Limitler
    private const int MaxBookingRequestsPerHour  = 10;  // IP başına saatte max booking isteği
    private const int MaxLoginAttemptsPerWindow  = 5;   // IP başına 15 dakikada max login denemesi
    private const int MaxLookupRequestsPerMinute = 5;   // IP başına dakikada max customer-lookup isteği
    private const int MaxOtpSendPerHour          = 5;   // IP başına saatte max OTP gönderme
    private const int MaxOtpVerifyPerWindow      = 10;  // IP başına 15 dakikada max kod doğrulama denemesi
    private const int MaxGeneralRequestsPerMinute = 150; // Genel API: IP başına dakikada max istek

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

        // OTP send rate limit — saatte 5 istek
        if (context.Request.Path.StartsWithSegments("/api/otp/send") &&
            context.Request.Method == "POST")
        {
            var otpKey = $"ratelimit:otp:{ip}:{DateTime.UtcNow:yyyyMMddHH}";
            var otpCount = _cache.GetOrCreate(otpKey, entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddHours(1);
                return 0;
            });

            if (otpCount >= MaxOtpSendPerHour)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Çok fazla doğrulama kodu isteği. 1 saat bekleyin.\"}");
                return;
            }

            _cache.Set(otpKey, otpCount + 1, DateTimeOffset.UtcNow.AddHours(1));
        }

        // OTP doğrulama / şifre sıfırlama brute-force koruması — 15 dakikada 10 deneme
        var isVerifyPath =
            (context.Request.Path.StartsWithSegments("/api/otp/verify") ||
             context.Request.Path.StartsWithSegments("/api/auth/reset-password") ||
             context.Request.Path.StartsWithSegments("/api/auth/forgot-password")) &&
            context.Request.Method == "POST";
        if (isVerifyPath)
        {
            var window = DateTime.UtcNow.ToString("yyyyMMddHHmm")[..11]; // 15 dakikalık pencere
            var verifyKey = $"ratelimit:otpverify:{ip}:{window}";
            var verifyCount = _cache.GetOrCreate(verifyKey, entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(15);
                return 0;
            });

            if (verifyCount >= MaxOtpVerifyPerWindow)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Çok fazla deneme. Lütfen 15 dakika bekleyin.\"}");
                return;
            }

            _cache.Set(verifyKey, verifyCount + 1, DateTimeOffset.UtcNow.AddMinutes(15));
        }

        // Customer lookup rate limit — dakikada 5 GET isteği (telefon numarası tarama koruması)
        if (context.Request.Path.Value?.Contains("/customer-lookup") == true &&
            context.Request.Method == "GET")
        {
            var minute = DateTime.UtcNow.ToString("yyyyMMddHHmm");
            var lookupKey = $"ratelimit:lookup:{ip}:{minute}";
            var lookupCount = _cache.GetOrCreate(lookupKey, entry =>
            {
                entry.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(1);
                return 0;
            });

            if (lookupCount >= MaxLookupRequestsPerMinute)
            {
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    "{\"success\":false,\"message\":\"Çok fazla istek. Lütfen bekleyin.\"}");
                return;
            }

            _cache.Set(lookupKey, lookupCount + 1, DateTimeOffset.UtcNow.AddMinutes(1));
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

        // ── Genel API rate limit ─────────────────────────────────────────
        // Daha spesifik limiti olan endpoint'ler yukarıda zaten kontrol edildi.
        // Burada kalan tüm /api/ yollarına genel dakikalık limit uygulanır.
        if (context.Request.Path.StartsWithSegments("/api"))
        {
            var isSpecificLimitApplied =
                (context.Request.Path.StartsWithSegments("/api/auth/login") && context.Request.Method == "POST") ||
                (context.Request.Path.StartsWithSegments("/api/otp/send")   && context.Request.Method == "POST") ||
                (context.Request.Path.Value?.Contains("/customer-lookup") == true && context.Request.Method == "GET") ||
                (context.Request.Path.StartsWithSegments("/api/booking")    && context.Request.Method == "POST");

            if (!isSpecificLimitApplied)
            {
                var minute     = DateTime.UtcNow.ToString("yyyyMMddHHmm");
                var globalKey  = $"ratelimit:global:{ip}:{minute}";
                var globalCount = _cache.GetOrCreate(globalKey, entry =>
                {
                    entry.AbsoluteExpiration = DateTime.UtcNow.AddMinutes(1);
                    return 0;
                });

                if (globalCount >= MaxGeneralRequestsPerMinute)
                {
                    context.Response.StatusCode  = (int)HttpStatusCode.TooManyRequests;
                    context.Response.ContentType = "application/json";
                    await context.Response.WriteAsync(
                        "{\"success\":false,\"message\":\"İstek limiti aşıldı. Lütfen bir dakika bekleyin.\"}");
                    return;
                }

                _cache.Set(globalKey, globalCount + 1, DateTimeOffset.UtcNow.AddMinutes(1));
            }
        }

        await _next(context);
    }

    private static string GetClientIp(HttpContext context)
    {
        // X-Real-IP: nginx tarafından $remote_addr ile set edilir, client tarafından manipüle edilemez.
        // X-Forwarded-For kullanılmıyor — client header injection saldırısına açık olduğu için.
        var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrEmpty(realIp))
            return realIp;

        // Geliştirme ortamı veya doğrudan bağlantı
        return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }
}