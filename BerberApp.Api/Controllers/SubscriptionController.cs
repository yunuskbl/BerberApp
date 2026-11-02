using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Pricing;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Api.Controllers;

[Route("api/subscription")]
public class SubscriptionController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly IIyzicoService _iyzico;
    private readonly IEmailService _email;
    private readonly string _frontendSuccessUrl;
    private readonly string _frontendFailUrl;

    public SubscriptionController(IMediator mediator, IAppDbContext context, IIyzicoService iyzico, IEmailService email, IConfiguration config)
        : base(mediator)
    {
        _context            = context;
        _iyzico             = iyzico;
        _email              = email;
        _frontendSuccessUrl = config["Iyzico:FrontendSuccessUrl"] ?? "https://ayarliyo.com/payment/success";
        _frontendFailUrl    = config["Iyzico:FrontendFailUrl"]    ?? "https://ayarliyo.com/payment/fail";
    }

    // GET /api/subscription
    [HttpGet]
    public async Task<IActionResult> GetSubscription(CancellationToken ct)
    {
        var sub = await _context.Subscriptions
            .Where(s => s.TenantId == TenantId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(ct);

        if (sub == null)
            return Success<object?>(null, "Aktif abonelik bulunamadı.");

        return Success(new
        {
            plan        = sub.Plan.ToString(),
            status      = sub.Status.ToString(),
            expiryDate  = sub.ExpiryDate,
            price       = sub.Price,
            currency    = sub.Currency,
            autoRenewal = sub.IsAutoRenewal,
            isActive    = sub.IsActive
        });
    }

    // POST /api/subscription/checkout
    [HttpPost("checkout")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> InitCheckout([FromBody] CheckoutRequest req, CancellationToken ct)
    {
        var planKey = req.Plan.ToLower();
        if (!PlanPricing.IsKnownPlan(planKey))
            return BadRequest(new { success = false, message = "Geçersiz plan." });

        // Fiyat ve süre periyottan geliyor; bu uç da aynı tek kaynağı okuyor.
        var period = PlanPricing.Parse(req.Billing);
        var price  = PlanPricing.PriceFor(planKey, period);

        var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.Id == TenantId, ct);
        if (tenant == null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.TenantId == TenantId && u.Role == UserRole.Admin, ct);
        if (user == null)
            return NotFound(new { success = false, message = "Kullanıcı bulunamadı." });

        var result = await _iyzico.InitiateCheckout(new IyzicoCheckoutRequest(
            TenantId:   TenantId,
            TenantName: tenant.Name,
            BuyerEmail: user.Email,
            BuyerName:  user.FirstName,
            BuyerSurname: user.LastName,
            BuyerPhone: tenant.Phone ?? user.Phone ?? "05000000000",
            Plan:       planKey,
            Price:      price,
            Period:     period
        ));

        if (!result.Success)
            return BadRequest(new { success = false, message = result.ErrorMessage });

        // Pending subscription kaydı oluştur
        var planType = planKey switch
        {
            "profesyonel" => PlanType.Profesyonel,
            "premium"     => PlanType.Premium,
            _             => PlanType.Baslangic
        };

        _context.Subscriptions.Add(new Subscription
        {
            TenantId             = TenantId,
            Plan                 = planType,
            StartDate            = DateTime.UtcNow,
            ExpiryDate           = PlanPricing.ExpiryFrom(DateTime.UtcNow, period),
            Status               = SubscriptionStatus.Pending,
            Price                = price,
            Currency             = "TRY",
            // Yinelenen çekim kurulu değil; ödeme sonrası da false kalıyor.
            IsAutoRenewal        = false,
            IyzicoConversationId = result.ConversationId
        });
        await _context.SaveChangesAsync(ct);

        return Success(new { checkoutFormContent = result.CheckoutFormContent });
    }

    // POST /api/subscription/callback  (İyzico'dan gelir — auth yok)
    [AllowAnonymous]
    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromForm] IyzicoCallbackPayload payload, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(payload.Token))
            return Redirect($"{_frontendFailUrl}?reason=no_token");

        var verification = await _iyzico.ValidateCallback(payload.Token);

        if (!verification.Success)
            return Redirect($"{_frontendFailUrl}?reason=payment_failed");

        var sub = await _context.Subscriptions
            .Where(s => s.IyzicoConversationId == verification.ConversationId && s.Status == SubscriptionStatus.Pending)
            .FirstOrDefaultAsync(ct);

        if (sub == null)
            return Redirect($"{_frontendFailUrl}?reason=not_found");

        // Eski aktif aboneliği iptal et
        var oldSubs = await _context.Subscriptions
            .Where(s => s.TenantId == sub.TenantId && s.Status == SubscriptionStatus.Active)
            .ToListAsync(ct);
        foreach (var old in oldSubs)
            old.Status = SubscriptionStatus.Cancelled;

        // Aktifleştir
        // Süre ödenen periyoda göre; sabit bir ay vermek yıllık ödeyeni
        // on bir ay eksik bırakıyordu.
        var activatedAt = DateTime.UtcNow;
        sub.Status          = SubscriptionStatus.Active;
        sub.StartDate       = activatedAt;
        sub.ExpiryDate      = PlanPricing.ExpiryFrom(activatedAt, verification.Period);
        sub.IyzicoPaymentId = verification.PaymentId;
        sub.IsAutoRenewal   = false;

        await _context.SaveChangesAsync(ct);

        var adminUser = await _context.Users
            .Where(u => u.TenantId == sub.TenantId && u.Role == UserRole.Admin)
            .FirstOrDefaultAsync(ct);
        if (adminUser != null)
            _ = _email.SendSubscriptionActivatedAsync(
                    adminUser.Email,
                    $"{adminUser.FirstName} {adminUser.LastName}",
                    sub.Plan.ToString(),
                    sub.ExpiryDate);

        return Redirect($"{_frontendSuccessUrl}?plan={sub.Plan}");
    }

    // DELETE /api/subscription
    [HttpDelete]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CancelSubscription(CancellationToken ct)
    {
        var sub = await _context.Subscriptions
            .Where(s => s.TenantId == TenantId && s.Status == SubscriptionStatus.Active)
            .FirstOrDefaultAsync(ct);

        if (sub == null)
            return NotFound(new { success = false, message = "Aktif abonelik bulunamadı." });

        sub.Status        = SubscriptionStatus.Cancelled;
        sub.IsAutoRenewal = false;
        await _context.SaveChangesAsync(ct);

        return Success<object>(null!, "Abonelik iptal edildi. Süre sonuna kadar erişim devam eder.");
    }
}

public record CheckoutRequest(
    string Plan,
    // "monthly" (varsayılan) ya da "annual".
    string? Billing = null
);

public class IyzicoCallbackPayload
{
    public string? Token { get; set; }
    public string? Status { get; set; }
}
