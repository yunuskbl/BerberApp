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
    private readonly IEmailService _emailService;
    private readonly ISmsService _smsService;

    public OtpController(IAppDbContext context, IEmailService emailService, ISmsService smsService)
    {
        _context = context;
        _emailService = emailService;
        _smsService = smsService;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { success = false, message = "Telefon numarası gerekli." });

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

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            await _emailService.SendOtpAsync(request.Email, otp);
            return Ok(new { success = true, message = "Doğrulama kodu e-posta adresinize gönderildi." });
        }
        else
        {
            await _smsService.SendOtpAsync(request.Phone, otp);
            return Ok(new { success = true, message = "Doğrulama kodu SMS ile gönderildi." });
        }
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

public class SendOtpRequest
{
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
}
public class VerifyOtpRequest { public string Phone { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }
