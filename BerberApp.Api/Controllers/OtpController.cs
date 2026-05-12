using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using BerberApp.Application.Common.Interfaces;
using BerberApp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/otp")]
[AllowAnonymous]
public class OtpController : ControllerBase
{
    private readonly IMemoryCache _cache;
    private readonly SmsService _smsService;

    public OtpController(IMemoryCache cache, SmsService smsService)
    {
        _cache = cache;
        _smsService = smsService;
    }

    // OTP gönderme endpoint'i — WhatsApp üzerinden gönderilir
    [HttpPost("send")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { success = false, message = "Telefon numarası gerekli." });

        // 6 haneli OTP oluştur (kriptografik güvenli)
        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var cacheKey = $"otp:{request.Phone}";

        // 5 dakika geçerli
        _cache.Set(cacheKey, otp, TimeSpan.FromMinutes(5));

        try
        {
            await _smsService.SendOtpAsync(request.Phone, otp);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Kod gönderilemedi: {ex.Message}" });
        }

        return Ok(new { success = true, message = "Doğrulama kodu SMS ile gönderildi." });
    }

    [HttpPost("verify")]
    public IActionResult VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var cacheKey = $"otp:{request.Phone}";

        if (!_cache.TryGetValue(cacheKey, out string? storedOtp))
            return BadRequest(new { success = false, message = "Kod süresi dolmuş veya geçersiz." });

        if (storedOtp != request.Code)
            return BadRequest(new { success = false, message = "Hatalı kod." });

        // Doğrulama başarılı — kodu sil
        _cache.Remove(cacheKey);

        // Doğrulanmış telefonu cache'e ekle (30 dakika geçerli)
        _cache.Set($"verified:{request.Phone}", true, TimeSpan.FromMinutes(30));

        return Ok(new { success = true, message = "Telefon doğrulandı." });
    }
}

public class SendOtpRequest { public string Phone { get; set; } = string.Empty; }
public class VerifyOtpRequest { public string Phone { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }