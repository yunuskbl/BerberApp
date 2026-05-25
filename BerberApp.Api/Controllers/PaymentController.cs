using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using BerberApp.Infrastructure.Services;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : BaseApiController
{
    private readonly IIyzicoService _iyzico;
    private readonly IyzicoService _iyzicoImpl;
    private readonly IAppDbContext _context;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        IMediator mediator,
        IIyzicoService iyzico,
        IyzicoService iyzicoImpl,
        IAppDbContext context,
        ILogger<PaymentController> logger)
        : base(mediator)
    {
        _iyzico     = iyzico;
        _iyzicoImpl = iyzicoImpl;
        _context    = context;
        _logger     = logger;
    }

    // ─── Checkout başlat ──────────────────────────────────────────────────────
    [Authorize]
    [HttpPost("initiate")]
    public async Task<IActionResult> Initiate([FromBody] InitiateRequest req)
    {
        req = req with { Plan = req.Plan.ToLower() };
        var price = IyzicoService.GetPrice(req.Plan);
        if (price == 0)
            return BadRequest(new { success = false, message = "Geçersiz plan." });

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == TenantId)
            .Select(t => new { t.Name, t.Phone })
            .FirstOrDefaultAsync();

        var email   = User.Claims.FirstOrDefault(c => c.Type == "email")?.Value ?? "";
        var name    = User.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value ?? "Salon";
        var surname = User.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value ?? "Admini";

        var result = await _iyzico.InitiateCheckout(new IyzicoCheckoutRequest(
            TenantId:     TenantId,
            TenantName:   tenant?.Name ?? "",
            BuyerEmail:   email,
            BuyerName:    name,
            BuyerSurname: surname,
            BuyerPhone:   tenant?.Phone ?? "+905000000000",
            Plan:         req.Plan,
            Price:        price
        ));

        if (!result.Success)
        {
            _logger.LogError("iyzico checkout başlatılamadı: {Error}", result.ErrorMessage);
            return BadRequest(new { success = false, message = result.ErrorMessage });
        }

        return Ok(new { success = true, data = new { result.CheckoutFormContent, result.ConversationId, price } });
    }

    // ─── Callback ─────────────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromForm] string token)
    {
        var successUrl = _iyzicoImpl.FrontendSuccessUrl;
        var failUrl    = _iyzicoImpl.FrontendFailUrl;

        try
        {
            var result = await _iyzico.ValidateCallback(token);

            if (!result.Success)
            {
                _logger.LogWarning("iyzico ödeme başarısız: {Error}", result.ErrorMessage);

                // Başarısız işlemi logla
                var failedTx = BuildTransaction(result, 0, PaymentTransactionStatus.Failed);
                _context.PaymentTransactions.Add(failedTx);
                await _context.SaveChangesAsync();

                return Redirect($"{failUrl}?reason={Uri.EscapeDataString(result.ErrorMessage ?? "Bilinmeyen hata")}");
            }

            var plan  = Enum.TryParse<PlanType>(result.Plan, true, out var p) ? p : PlanType.Baslangic;
            var price = IyzicoService.GetPrice(result.Plan ?? "baslangic");
            var now   = DateTime.UtcNow;

            // Subscription güncelle / oluştur
            var existing = await _context.Subscriptions
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == result.TenantId)
                .OrderByDescending(s => s.StartDate)
                .FirstOrDefaultAsync();

            if (existing != null)
            {
                existing.Plan       = plan;
                existing.Status     = SubscriptionStatus.Active;
                existing.StartDate  = now;
                existing.ExpiryDate = now.AddYears(1);
                existing.Price      = price;
            }
            else
            {
                _context.Subscriptions.Add(new Subscription
                {
                    TenantId      = result.TenantId,
                    Plan          = plan,
                    Status        = SubscriptionStatus.Active,
                    StartDate     = now,
                    ExpiryDate    = now.AddYears(1),
                    Price         = price,
                    Currency      = "TRY",
                    IsAutoRenewal = false
                });
            }

            // Başarılı işlemi logla
            _context.PaymentTransactions.Add(BuildTransaction(result, price, PaymentTransactionStatus.Success));
            await _context.SaveChangesAsync();

            _logger.LogInformation("Ödeme başarılı: Tenant {TenantId}, Plan {Plan}", result.TenantId, result.Plan);
            return Redirect($"{successUrl}?plan={result.Plan}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Callback işlenirken hata oluştu");
            return Redirect($"{failUrl}?reason=sistem-hatasi");
        }
    }

    // ─── İade (super-admin) ───────────────────────────────────────────────────
    [Authorize(Roles = "SuperAdmin")]
    [HttpPost("refund/{transactionId:guid}")]
    public async Task<IActionResult> Refund(Guid transactionId)
    {
        var tx = await _context.PaymentTransactions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == transactionId);

        if (tx is null)
            return NotFound(new { success = false, message = "İşlem bulunamadı." });

        if (tx.Status != PaymentTransactionStatus.Success)
            return BadRequest(new { success = false, message = "Sadece başarılı işlemler iade edilebilir." });

        if (string.IsNullOrEmpty(tx.IyzicoPaymentTransactionId))
            return BadRequest(new { success = false, message = "Iyzico işlem kimliği eksik." });

        var refund = await _iyzico.RefundPayment(tx.IyzicoPaymentTransactionId, tx.Amount);

        if (!refund.Success)
        {
            _logger.LogError("İade başarısız: {TransactionId} — {Error}", transactionId, refund.ErrorMessage);
            return BadRequest(new { success = false, message = refund.ErrorMessage });
        }

        tx.Status     = PaymentTransactionStatus.Refunded;
        tx.RefundedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        _logger.LogInformation("İade başarılı: {TransactionId}", transactionId);
        return Ok(new { success = true });
    }

    // ─── İşlem geçmişi (super-admin) ─────────────────────────────────────────
    [Authorize(Roles = "SuperAdmin")]
    [HttpGet("transactions")]
    public async Task<IActionResult> GetAllTransactions(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var query = _context.PaymentTransactions
            .IgnoreQueryFilters()
            .Include(t => t.Tenant)
            .OrderByDescending(t => t.CreatedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new
            {
                t.Id, t.CreatedAt,
                TenantName       = t.Tenant.Name,
                t.Plan,  t.Amount, t.Currency, t.Status,
                t.ConversationId, t.IyzicoPaymentId,
                t.ErrorMessage,  t.RefundedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = items, total, page, pageSize });
    }

    // ─── Kendi işlem geçmişim (salon admini) ─────────────────────────────────
    [Authorize]
    [HttpGet("my-transactions")]
    public async Task<IActionResult> GetMyTransactions()
    {
        var items = await _context.PaymentTransactions
            .IgnoreQueryFilters()
            .Where(t => t.TenantId == TenantId)
            .OrderByDescending(t => t.CreatedAt)
            .Select(t => new
            {
                t.Id, t.CreatedAt,
                t.Plan, t.Amount, t.Currency, t.Status,
                t.RefundedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = items });
    }

    // ─── Fiyatlar ─────────────────────────────────────────────────────────────
    [AllowAnonymous]
    [HttpGet("prices")]
    public IActionResult GetPrices() => Ok(new
    {
        success = true,
        data = new[]
        {
            new { plan = "baslangic",   price = 899m  },
            new { plan = "profesyonel", price = 1799m },
            new { plan = "premium",     price = 2399m },
        }
    });

    // ─── Yardımcı ─────────────────────────────────────────────────────────────
    private static PaymentTransaction BuildTransaction(
        IyzicoPaymentResult result,
        decimal amount,
        PaymentTransactionStatus status) => new()
    {
        TenantId                   = result.TenantId,
        Plan                       = result.Plan ?? "",
        Amount                     = amount,
        Currency                   = "TRY",
        Status                     = status,
        ConversationId             = result.ConversationId,
        IyzicoPaymentId            = result.PaymentId,
        IyzicoPaymentTransactionId = result.PaymentTransactionId,
        ErrorMessage               = result.ErrorMessage
    };
}

public record InitiateRequest(string Plan);
