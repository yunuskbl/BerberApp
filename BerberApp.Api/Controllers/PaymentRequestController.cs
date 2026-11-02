using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Pricing;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/payment-request")]
[Authorize(Roles = "Admin,SuperAdmin")]
public class PaymentRequestController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly ILogger<PaymentRequestController> _logger;

    public PaymentRequestController(IMediator mediator, IAppDbContext context, ILogger<PaymentRequestController> logger)
        : base(mediator)
    {
        _context = context;
        _logger  = logger;
    }

    /// <summary>
    /// İşletme havale talebini gönderir ve referans kodu alır
    /// </summary>
    [HttpPost("submit")]
    public async Task<IActionResult> Submit([FromBody] SubmitPaymentRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.PlanName))
            return BadRequest(new { success = false, message = "Plan adı boş olamaz." });

        var planName = dto.PlanName.Trim().ToLowerInvariant();
        if (!PlanPricing.IsKnownPlan(planName))
            return BadRequest(new { success = false, message = "Geçersiz plan." });

        if (dto.DurationDays != 30 && dto.DurationDays != 365)
            return BadRequest(new { success = false, message = "Geçersiz abonelik süresi." });

        var tenantId = TenantId;

        // Tutar sunucuda hesaplanıyor; istemciden geleni kaydetmek, tarayıcıdan
        // {amount: 1} gönderip onay ekranına ₺1'lik Premium talebi düşürmeye
        // açık bir kapı bırakıyordu.
        var period = PlanPricing.FromDurationDays(dto.DurationDays);
        var amount = PlanPricing.PriceFor(planName, period);

        if (dto.Amount != amount)
        {
            _logger.LogWarning(
                "[ÖDEME] İstemci tutarı sunucu fiyatıyla uyuşmuyor. Tenant={TenantId} " +
                "Plan={Plan} Süre={Days} İstemci={Client} Sunucu={Server}",
                tenantId, planName, dto.DurationDays, dto.Amount, amount);
        }

        var existing = await _context.PaymentRequests
            .Where(r => r.TenantId == tenantId && r.PlanName == planName && r.DurationDays == dto.DurationDays && r.Status == PaymentRequestStatus.Pending)
            .FirstOrDefaultAsync();

        if (existing != null)
            return Ok(new { success = true, referenceCode = existing.ReferenceCode, message = "Bekleyen bir talebiniz zaten mevcut." });

        var refCode = GenerateReferenceCode();

        var request = new PaymentRequest
        {
            Id            = Guid.NewGuid(),
            TenantId      = tenantId,
            PlanName      = planName,
            PlanLabel     = dto.PlanLabel,
            Amount        = amount,
            DurationDays  = dto.DurationDays,
            ReferenceCode = refCode,
            Status        = PaymentRequestStatus.Pending,
            CreatedAt     = DateTime.UtcNow
        };

        _context.PaymentRequests.Add(request);
        await _context.SaveChangesAsync();

        _logger.LogInformation("PaymentRequest created for tenant {TenantId}, plan {Plan}, ref {Ref}", tenantId, dto.PlanName, refCode);

        return Ok(new { success = true, referenceCode = refCode, message = "Ödeme talebiniz alındı." });
    }

    /// <summary>
    /// İşletmenin kendi ödeme taleplerini listeler
    /// </summary>
    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var tenantId = TenantId;

        var requests = await _context.PaymentRequests
            .Where(r => r.TenantId == tenantId)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.PlanName,
                r.PlanLabel,
                r.Amount,
                r.ReferenceCode,
                Status = r.Status.ToString(),
                r.AdminNotes,
                r.ReviewedAt,
                r.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = requests });
    }

    private static string GenerateReferenceCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var rng = new Random();
        var code = new string(Enumerable.Range(0, 6).Select(_ => chars[rng.Next(chars.Length)]).ToArray());
        return $"AYR-{code}";
    }
}

public class SubmitPaymentRequestDto
{
    public string PlanName   { get; set; } = string.Empty;
    public string PlanLabel  { get; set; } = string.Empty;
    /// <summary>
    /// İstemcinin gösterdiği tutar. Yalnızca karşılaştırma ve kayıt amaçlı;
    /// kaydedilen tutar sunucuda <see cref="PlanPricing"/> ile hesaplanır.
    /// </summary>
    public decimal Amount    { get; set; }
    public int DurationDays  { get; set; } = 30;
}
