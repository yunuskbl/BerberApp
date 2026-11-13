using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R = BerberApp.Application.Common.Services.SessionPackageRules;

namespace BerberApp.Api.Controllers;

/// <summary>
/// Seans paketleri — müşteri tarafı, giriş istemez.
///
/// Erişim tahmin edilemez anahtarlarla:
///  - paket takip sayfası <c>CustomerPackage.PublicToken</c> (müşteri kartındaki QR),
///  - seans onayı <c>PackageSession.ConfirmCode</c> (salonda ekranda gösterilen,
///    15 dakika geçerli QR).
/// Müşteriye açık yanıtlarda personelin ve işletmenin iç notları, iptal
/// gerekçeleri ve müşterinin tam adı/telefonu yer almaz.
/// </summary>
[ApiController]
[AllowAnonymous]
[Route("api/public/packages")]
public class PublicPackagesController : ControllerBase
{
    private readonly IAppDbContext _context;

    public PublicPackagesController(IAppDbContext context) => _context = context;

    /// <summary>Müşterinin paket takip sayfası.</summary>
    [HttpGet("{token}")]
    public async Task<IActionResult> GetPackage(string token, CancellationToken ct)
    {
        if (!LooksLikeToken(token)) return NotFoundResponse("Paket bulunamadı.");

        var p = await _context.CustomerPackages.AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Customer)
            .Include(x => x.Service)
            .Include(x => x.Sessions).ThenInclude(s => s.Staff)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.PublicToken == token, ct);
        if (p is null) return NotFoundResponse("Paket bulunamadı.");

        var now  = DateTime.UtcNow;
        var used = R.UsedSessions(p.Sessions);
        var paid = R.Paid(p.Payments);

        return Ok(new
        {
            success = true,
            data = new
            {
                salon = SalonInfo(p.Tenant),
                customerFirstName = R.FirstName(p.Customer?.FullName),
                package = new
                {
                    p.Name,
                    serviceName       = p.Service?.Name,
                    p.TotalSessions,
                    usedSessions      = used,
                    remainingSessions = Math.Max(0, p.TotalSessions - used),
                    p.Status,
                    isExpired         = R.IsExpired(p.Status, p.ExpiresAt, now),
                    p.StartedAt,
                    p.ExpiresAt,
                    p.Price,
                    paid,
                    remainingDebt     = R.RemainingDebt(p.Price, paid),
                    p.Currency,
                },
                sessions = R.Numbered(p.Sessions)
                    .Select(x => new
                    {
                        number      = x.Number,
                        performedAt = x.Session.PerformedAt,
                        staffName   = R.ShortName(x.Session.Staff?.FullName),
                        confirmed   = x.Session.CustomerConfirmedAt != null,
                        isVoid      = x.Session.IsVoid,
                    })
                    .Reverse()
                    .ToList(),
                payments = p.Payments
                    .Where(x => !x.IsVoid)
                    .OrderByDescending(x => x.PaidAt)
                    .Select(x => new { x.Amount, x.PaidAt, x.Method })
                    .ToList(),
            },
        });
    }

    /// <summary>Seans onay sayfası: QR okutulunca hangi seansın onaylanacağını gösterir.</summary>
    [HttpGet("sessions/{code}")]
    public async Task<IActionResult> GetSession(string code, CancellationToken ct)
    {
        var s = await FindSessionAsync(code, ct);
        if (s is null) return NotFoundResponse("Onay kodu geçersiz.");
        return Ok(new { success = true, data = SessionInfo(s, DateTime.UtcNow) });
    }

    /// <summary>
    /// Müşteri seansı onaylar. Seans zaten düşmüş durumda; onay yalnızca
    /// "müşteri onayladı" işaretini koyar. Aynı kod tekrar okutulursa sonuç değişmez.
    /// </summary>
    [HttpPost("sessions/{code}/confirm")]
    public async Task<IActionResult> ConfirmSession(string code, CancellationToken ct)
    {
        var s = await FindSessionAsync(code, ct);
        if (s is null) return NotFoundResponse("Onay kodu geçersiz.");

        var now = DateTime.UtcNow;
        if (s.IsVoid)
            return BadRequest(new { success = false, message = "Bu seans işletme tarafından iptal edilmiş." });

        if (s.CustomerConfirmedAt is null)
        {
            if (s.ConfirmCodeExpiresAt is not { } exp || exp < now)
                return BadRequest(new { success = false, message = "Onay kodunun süresi doldu. Personelden yeni kod göstermesini isteyin." });

            s.CustomerConfirmedAt = now;
            await _context.SaveChangesAsync(ct);
        }

        return Ok(new { success = true, message = "Seans onaylandı.", data = SessionInfo(s, now) });
    }

    /// <summary>
    /// Seansı, numarasını hesaplamak için paketin diğer seanslarıyla birlikte yükler.
    /// Seans → paket → seanslar bir döngü; EF bunu izlenmeyen (AsNoTracking)
    /// sorguda reddediyor, bu yüzden sorgu izlenen.
    /// </summary>
    private async Task<PackageSession?> FindSessionAsync(string code, CancellationToken ct)
    {
        if (!LooksLikeToken(code)) return null;
        return await _context.PackageSessions
            .Include(s => s.Staff)
            .Include(s => s.CustomerPackage).ThenInclude(p => p.Tenant)
            .Include(s => s.CustomerPackage).ThenInclude(p => p.Sessions)
            .FirstOrDefaultAsync(s => s.ConfirmCode == code, ct);
    }

    private static object SessionInfo(PackageSession s, DateTime now)
    {
        var pkg = s.CustomerPackage;
        var number = R.Numbered(pkg.Sessions).FirstOrDefault(x => x.Session.Id == s.Id).Number;
        return new
        {
            salon           = SalonInfo(pkg.Tenant),
            packageName     = pkg.Name,
            sessionNumber   = number,
            totalSessions   = pkg.TotalSessions,
            performedAt     = s.PerformedAt,
            staffName       = R.ShortName(s.Staff?.FullName),
            confirmed       = s.CustomerConfirmedAt != null,
            confirmedAt     = s.CustomerConfirmedAt,
            isVoid          = s.IsVoid,
            expired         = s.CustomerConfirmedAt is null && (s.ConfirmCodeExpiresAt is not { } e || e < now),
            // Onay sayfasından "paketimi takip et" bağlantısı için. Kodu okutan,
            // salonda ekranı gören müşterinin kendisi.
            packageToken    = pkg.PublicToken,
        };
    }

    private static object SalonInfo(Tenant? t) => new
    {
        name      = t?.Name ?? string.Empty,
        subdomain = t?.Subdomain ?? string.Empty,
        logoUrl   = t?.LogoUrl,
        phone     = t?.Phone,
        address   = t?.Address,
    };

    /// <summary>Anahtarlar 32 karakterlik URL-güvenli metin; başka bir şey veritabanına hiç sorulmaz.</summary>
    private static bool LooksLikeToken(string? s)
        => s is { Length: >= 20 and <= 64 } && s.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    private IActionResult NotFoundResponse(string message) => NotFound(new { success = false, message });
}
