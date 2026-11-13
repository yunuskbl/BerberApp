using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using BerberApp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BerberApp.Api.Controllers;

/// <summary>
/// Cihaz bazında anlık bildirim aboneliği (panel → Ayarlar → Bildirimler).
/// </summary>
[Authorize(Roles = "Admin,Staff")]
public class PushController : BaseApiController
{
    /// <summary>
    /// Yalnızca tarayıcı üreticilerinin push sunucularına gönderim yapılır.
    /// Abonelik adresi istemciden geldiği için serbest bırakılsaydı sunucumuz
    /// istenen herhangi bir adrese istek atan bir araca dönüşürdü (SSRF).
    /// </summary>
    private static readonly string[] AllowedPushHosts =
    [
        "fcm.googleapis.com",            // Chrome, Android, Samsung, Opera
        "android.googleapis.com",
        "push.services.mozilla.com",     // Firefox (updates.push.services.mozilla.com)
        "push.apple.com",                // Safari, iOS ana ekran uygulaması (web.push.apple.com)
        "notify.windows.com",            // Edge (*.notify.windows.com)
    ];

    private readonly IAppDbContext _context;
    private readonly WebPushSettings _cfg;
    private readonly IWebHostEnvironment _env;

    public PushController(IMediator mediator, IAppDbContext context, IOptions<WebPushSettings> cfg, IWebHostEnvironment env)
        : base(mediator)
    {
        _context = context;
        _cfg = cfg.Value;
        _env = env;
    }

    [HttpGet("config")]
    public IActionResult Config()
        => Success(new { enabled = _cfg.IsConfigured, publicKey = _cfg.IsConfigured ? _cfg.PublicKey : null });

    [HttpPost("subscribe")]
    public async Task<IActionResult> Subscribe([FromBody] PushSubscribeRequest req, CancellationToken ct)
    {
        if (!_cfg.IsConfigured)
            return BadRequest(new { success = false, message = "Anlık bildirimler sunucuda henüz yapılandırılmamış." });
        if (!IsAllowedEndpoint(req.Endpoint))
            return BadRequest(new { success = false, message = "Bu tarayıcının bildirim adresi desteklenmiyor." });
        if (string.IsNullOrWhiteSpace(req.Keys?.P256dh) || string.IsNullOrWhiteSpace(req.Keys?.Auth) ||
            req.Keys.P256dh.Length > 128 || req.Keys.Auth.Length > 64)
            return BadRequest(new { success = false, message = "Abonelik anahtarları eksik." });

        var sub = await _context.PushSubscriptions.FirstOrDefaultAsync(s => s.Endpoint == req.Endpoint, ct);
        if (sub is null)
        {
            sub = new PushSubscription { Endpoint = req.Endpoint! };
            _context.PushSubscriptions.Add(sub);
        }
        // Aynı cihazda başka hesapla giriş yapıldıysa abonelik yeni sahibine geçer.
        sub.TenantId     = TenantId;
        sub.UserId       = UserId;
        sub.P256dh       = req.Keys.P256dh;
        sub.Auth         = req.Keys.Auth;
        sub.IsStandalone = req.Standalone;
        sub.Platform     = req.Platform is { Length: <= 20 } pl ? pl : null;
        sub.FailureCount = 0;

        await _context.SaveChangesAsync(ct);
        return Success(new { subscribed = true }, "Bu cihazda bildirimler açıldı.");
    }

    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe([FromBody] PushUnsubscribeRequest req, CancellationToken ct)
    {
        var sub = await _context.PushSubscriptions
            .FirstOrDefaultAsync(s => s.Endpoint == req.Endpoint && s.UserId == UserId, ct);
        if (sub is not null)
        {
            _context.PushSubscriptions.Remove(sub);
            await _context.SaveChangesAsync(ct);
        }
        return Success(new { subscribed = false }, "Bu cihazda bildirimler kapatıldı.");
    }

    /// <summary>Kullanıcının kendi cihazlarına deneme bildirimi.</summary>
    [HttpPost("test")]
    public async Task<IActionResult> Test([FromServices] IPushNotificationService push, CancellationToken ct)
    {
        if (!push.IsEnabled)
            return BadRequest(new { success = false, message = "Anlık bildirimler sunucuda henüz yapılandırılmamış." });
        var delivered = await push.SendToUserAsync(UserId,
            new PushMessage("ayarlıyo", "Bildirimler bu cihazda çalışıyor ✓", "/dashboard", "test"), ct);
        return delivered > 0
            ? Success(new { delivered }, "Deneme bildirimi gönderildi.")
            : BadRequest(new { success = false, message = "Bildirim hiçbir cihaza ulaşmadı. Bildirimleri bu cihazda yeniden açmayı deneyin." });
    }

    private bool IsAllowedEndpoint(string? endpoint)
    {
        if (string.IsNullOrWhiteSpace(endpoint) || endpoint.Length > 1024 ||
            !Uri.TryCreate(endpoint, UriKind.Absolute, out var uri))
            return false;
        // Geliştirme ortamında yerel sahte push sunucusuyla test edilebilsin.
        if (_env.IsDevelopment() && uri.IsLoopback) return true;
        if (uri.Scheme != Uri.UriSchemeHttps) return false;
        return AllowedPushHosts.Any(h => uri.Host == h || uri.Host.EndsWith("." + h, StringComparison.OrdinalIgnoreCase));
    }
}

public record PushSubscribeRequest(string? Endpoint, PushKeys? Keys, bool Standalone = false, string? Platform = null);
public record PushKeys(string? P256dh, string? Auth);
public record PushUnsubscribeRequest(string? Endpoint);
