using Microsoft.AspNetCore.Mvc;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/otp")]
[AllowAnonymous]
public class OtpController : ControllerBase
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;

    public OtpController(IAppDbContext context, IWhatsAppService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { success = false, message = "Telefon numarası gerekli." });

        // Mevcut OTP kayıtlarını sil
        var existing = await _context.OtpRecords
            .Where(x => x.Phone == request.Phone)
            .ToListAsync();
        _context.OtpRecords.RemoveRange(existing);

        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        _context.OtpRecords.Add(new OtpRecord
        {
            Phone = request.Phone,
            Code = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
        await _context.SaveChangesAsync();

        try
        {
            await _whatsAppService.SendOtpAsync(request.Phone, otp);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, message = $"Kod gönderilemedi: {ex.Message}" });
        }

        return Ok(new { success = true, message = "Doğrulama kodu WhatsApp ile gönderildi." });
    }

    [HttpPost("verify")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        var record = await _context.OtpRecords
            .Where(x => x.Phone == request.Phone && !x.IsVerified && x.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(x => x.CreatedAt)
            .FirstOrDefaultAsync();

        if (record is null)
            return BadRequest(new { success = false, message = "Kod süresi dolmuş veya geçersiz." });

        if (record.Code != request.Code)
            return BadRequest(new { success = false, message = "Hatalı kod." });

        record.IsVerified = true;
        record.VerifiedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Telefon doğrulandı." });
    }
}

public class SendOtpRequest { public string Phone { get; set; } = string.Empty; }
public class VerifyOtpRequest { public string Phone { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }
