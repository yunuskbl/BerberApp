using Microsoft.AspNetCore.Mvc;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Validators;
using BerberApp.Domain.Entities;
using BerberApp.Infrastructure.Services;
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
    private readonly WppConnectWhatsAppService _waOtp;
    private readonly ILogger<OtpController> _logger;

    public OtpController(
        IAppDbContext context,
        IEmailService emailService,
        WppConnectWhatsAppService waOtp,
        ILogger<OtpController> logger)
    {
        _context = context;
        _emailService = emailService;
        _waOtp = waOtp;
        _logger = logger;
    }

    [HttpPost("send")]
    public async Task<IActionResult> SendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Phone))
            return BadRequest(new { success = false, message = "Telefon numarası gerekli." });

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            if (!TrustedEmailHelper.HasAllowedDomain(request.Email))
                return BadRequest(new { success = false, message = "Geçerli bir mail adresi giriniz." });
            if (!TrustedEmailHelper.HasPlausibleLocalPart(request.Email))
                return BadRequest(new { success = false, message = "Geçerli bir mail adresi giriniz." });
        }

        var existing = await _context.OtpRecords
            .Where(x => x.Phone == request.Phone)
            .ToListAsync();
        _context.OtpRecords.RemoveRange(existing);

        var otp = System.Security.Cryptography.RandomNumberGenerator.GetInt32(100000, 1000000).ToString();

        // İşletme adı yalnızca subdomain üzerinden veritabanından çözülür. İstemciden
        // gelen serbest metin doğrudan kullanılsaydı, herkes bizim gönderici başlığımız
        // altında istediği içeriği kullanıcıya yazdırabilirdi.
        var salonName = string.Empty;
        if (!string.IsNullOrWhiteSpace(request.Subdomain))
        {
            salonName = await _context.Tenants
                .AsNoTracking()
                .Where(t => t.Subdomain == request.Subdomain)
                .Select(t => t.Name)
                .FirstOrDefaultAsync() ?? string.Empty;
        }

        _context.OtpRecords.Add(new OtpRecord
        {
            Phone = request.Phone,
            Code = otp,
            ExpiresAt = DateTime.UtcNow.AddMinutes(5)
        });
        await _context.SaveChangesAsync();

        // E-posta verildiyse öncelik e-posta (ücretsiz, güvenilir)
        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            try
            {
                await _emailService.SendOtpAsync(request.Email, otp, salonName);
                return Ok(new { success = true, channel = "email", message = "Doğrulama kodu e-posta adresinize gönderildi." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[OTP] E-posta gönderilemedi: {Email}", request.Email);
                return StatusCode(502, new { success = false, message = "Doğrulama kodu gönderilemedi. Lütfen tekrar deneyin." });
            }
        }

        // Telefon: önce merkezi WPPConnect oturumu üzerinden WhatsApp dene
        // (SuperAdmin → WhatsApp bağlıysa). WppConnectWhatsAppService.SendOtpAsync
        // WhatsApp başarısız olursa kendi içinde otomatik SMS (Netgsm) fallback'i yapar.
        try
        {
            await _waOtp.SendOtpAsync(request.Phone, otp, salonName);
            return Ok(new { success = true, channel = "whatsapp_or_sms", message = "Doğrulama kodu gönderildi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[OTP] WhatsApp/SMS gönderilemedi: {Phone}", request.Phone);
            return StatusCode(502, new
            {
                success = false,
                message = "Doğrulama kodu gönderilemedi. Lütfen e-posta adresinizi girerek tekrar deneyin."
            });
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

        // Brute-force koruması: 5 hatalı denemeden sonra kodu geçersiz kıl
        const int maxAttempts = 5;
        if (record.FailedAttempts >= maxAttempts)
        {
            record.ExpiresAt = DateTime.UtcNow; // kodu anında geçersizleştir
            await _context.SaveChangesAsync();
            return BadRequest(new { success = false, message = "Çok fazla hatalı deneme. Lütfen yeni kod isteyin." });
        }

        if (record.Code != request.Code)
        {
            record.FailedAttempts++;
            await _context.SaveChangesAsync();
            return BadRequest(new { success = false, message = "Hatalı kod." });
        }

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

    /// <summary>
    /// Randevu akışındaki salonun subdomain'i. Doğrulama mesajında kodun hangi işletme
    /// için istendiğini yazabilmek için kullanılır; işletme adı sunucuda çözülür.
    /// İşletme kaydı gibi salon bağlamı olmayan akışlarda boş bırakılır.
    /// </summary>
    public string? Subdomain { get; set; }
}
public class VerifyOtpRequest { public string Phone { get; set; } = string.Empty; public string Code { get; set; } = string.Empty; }
