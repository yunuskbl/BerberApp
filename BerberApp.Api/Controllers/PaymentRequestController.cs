using BerberApp.Application.Common.Interfaces;
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
[Authorize]
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

        var tenantId = TenantId;

        // Aynı plan için bekleyen talep var mı kontrol et
        var existing = await _context.PaymentRequests
            .Where(r => r.TenantId == tenantId && r.PlanName == dto.PlanName && r.Status == PaymentRequestStatus.Pending)
            .FirstOrDefaultAsync();

        if (existing != null)
            return Ok(new { success = true, referenceCode = existing.ReferenceCode, message = "Bekleyen bir talebiniz zaten mevcut." });

        var refCode = GenerateReferenceCode();

        var request = new PaymentRequest
        {
            Id            = Guid.NewGuid(),
            TenantId      = tenantId,
            PlanName      = dto.PlanName,
            PlanLabel     = dto.PlanLabel,
            Amount        = dto.Amount,
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
    public string PlanName  { get; set; } = string.Empty;
    public string PlanLabel { get; set; } = string.Empty;
    public decimal Amount   { get; set; }
}
