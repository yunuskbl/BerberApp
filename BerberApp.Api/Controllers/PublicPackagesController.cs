using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Application.Common.Services;
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
        var booking  = await PackageBooking.GetStateAsync(_context, p, ct);
        var upcoming = await UpcomingAsync(p.Id, ct);

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
                    canBook            = booking.CanBook,
                    bookingBlockReason = booking.BlockReason,
                },
                upcoming,
                sessions = R.Numbered(p.Sessions)
                    .Select(x => new
                    {
                        id          = x.Session.Id,
                        number      = x.Number,
                        performedAt = x.Session.PerformedAt,
                        staffName   = R.ShortName(x.Session.Staff?.FullName),
                        confirmed   = x.Session.CustomerConfirmedAt != null,
                        isVoid      = x.Session.IsVoid,
                        canConfirm  = CanConfirmRemotely(x.Session, now),
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

    /// <summary>
    /// Paketten randevu sayfası için bağlam: hangi hizmet, kaçıncı seans,
    /// randevu alınabilir mi. Randevu sayfası paket kipine bununla geçer.
    /// </summary>
    [HttpGet("{token}/booking")]
    public async Task<IActionResult> GetBookingContext(string token, CancellationToken ct)
    {
        if (!LooksLikeToken(token)) return NotFoundResponse("Paket bulunamadı.");

        var p = await _context.CustomerPackages.AsNoTracking()
            .Include(x => x.Tenant)
            .Include(x => x.Customer)
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.PublicToken == token, ct);
        if (p is null) return NotFoundResponse("Paket bulunamadı.");

        var state = await PackageBooking.GetStateAsync(_context, p, ct);
        return Ok(new { success = true, data = BookingContext(p, state) });
    }

    /// <summary>
    /// Randevu sayfasında telefonunu doğrulayan müşterinin, o salondaki
    /// aktif paketleri. Yalnızca son bir saatte doğrulama kodu girilmiş
    /// numaraya cevap verir — numarayı bilen herkes paket bilgisini göremesin.
    /// </summary>
    [HttpPost("lookup")]
    public async Task<IActionResult> LookupByPhone([FromBody] PackageLookupRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Subdomain) || string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { success = false, message = "Eksik bilgi." });

        var verified = await _context.OtpRecords.AnyAsync(o =>
            o.Phone == req.Phone && o.IsVerified && o.VerifiedAt != null &&
            o.VerifiedAt > DateTime.UtcNow.AddHours(-1), ct);
        if (!verified)
            return StatusCode(403, new { success = false, message = "Telefon doğrulanmamış." });

        // Telefonlar farklı biçimlerde kayıtlı olabiliyor: randevu sayfası
        // "+905551112233" gönderiyor, işletmenin elle eklediği müşteri
        // "0555 111 22 33" olabilir. Rakamların son 10 hanesiyle eşleştir.
        // Salonun aktif paketleri az olduğu için süzme bellekte.
        var wanted = PhoneTail(req.Phone);
        if (wanted.Length < 7)
            return BadRequest(new { success = false, message = "Telefon numarası geçersiz." });

        var packages = (await _context.CustomerPackages.AsNoTracking()
                .Include(x => x.Tenant)
                .Include(x => x.Customer)
                .Include(x => x.Service)
                .Where(x => x.Tenant.Subdomain == req.Subdomain && x.Status == CustomerPackageStatus.Active)
                .OrderByDescending(x => x.StartedAt)
                .ToListAsync(ct))
            .Where(x => PhoneTail(x.Customer?.Phone) == wanted)
            .ToList();

        var list = new List<object>();
        foreach (var p in packages)
        {
            var state = await PackageBooking.GetStateAsync(_context, p, ct);
            list.Add(BookingContext(p, state));
        }
        return Ok(new { success = true, data = list });
    }

    /// <summary>
    /// Müşteri seansı takip sayfasından onaylar (salonda QR okutmadıysa).
    /// Takip anahtarı müşteriyi kanıtladığı için kod gerekmez; yalnızca son
    /// 30 günde yapılmış, iptal edilmemiş seanslar onaylanabilir.
    /// </summary>
    [HttpPost("{token}/sessions/{sessionId:guid}/confirm")]
    public async Task<IActionResult> ConfirmFromTrackingPage(
        string token, Guid sessionId, [FromServices] BerberApp.Api.Services.PushEvents push, CancellationToken ct)
    {
        if (!LooksLikeToken(token)) return NotFoundResponse("Paket bulunamadı.");

        var s = await _context.PackageSessions
            .Include(x => x.CustomerPackage).ThenInclude(p => p.Customer)
            .Include(x => x.CustomerPackage).ThenInclude(p => p.Sessions)
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.CustomerPackage.PublicToken == token, ct);
        if (s is null) return NotFoundResponse("Seans bulunamadı.");
        if (s.IsVoid) return BadRequest(new { success = false, message = "Bu seans salon tarafından iptal edilmiş." });

        if (s.CustomerConfirmedAt is null)
        {
            if (!CanConfirmRemotely(s, DateTime.UtcNow))
                return BadRequest(new { success = false, message = "Bu seans artık onaylanamıyor." });
            s.CustomerConfirmedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(ct);

            // Salonda QR okutulduğunda panel bunu zaten canlı gösteriyor; uzaktan
            // (takip sayfasından) verilen onayı işletme ancak bildirimle öğrenir.
            var pkg = s.CustomerPackage;
            var number = R.Numbered(pkg.Sessions).FirstOrDefault(x => x.Session.Id == s.Id).Number;
            push.PackageSessionConfirmed(pkg.TenantId, pkg.Id, R.FirstName(pkg.Customer?.FullName), pkg.Name, number);
        }
        return Ok(new { success = true, message = "Seans onaylandı." });
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

    /// <summary>Telefonun rakamlarının son 10 hanesi (ülke kodu ve baştaki 0 farkını yok sayar).</summary>
    private static string PhoneTail(string? phone)
    {
        var digits = new string((phone ?? string.Empty).Where(char.IsDigit).ToArray());
        return digits.Length > 10 ? digits[^10..] : digits;
    }

    /// <summary>Takip sayfasından onay penceresi: seans yapıldıktan sonraki 30 gün.</summary>
    private static readonly TimeSpan RemoteConfirmWindow = TimeSpan.FromDays(30);

    private static bool CanConfirmRemotely(PackageSession s, DateTime now)
        => !s.IsVoid && s.CustomerConfirmedAt is null && now - s.PerformedAt <= RemoteConfirmWindow;

    /// <summary>Paketten alınmış, henüz tamamlanmamış randevular (müşteriye "planlanan seanslar").</summary>
    private async Task<List<object>> UpcomingAsync(Guid packageId, CancellationToken ct)
    {
        var list = await _context.Appointments.AsNoTracking()
            .Where(a => a.CustomerPackageId == packageId &&
                        (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
            .OrderBy(a => a.StartTime)
            .Select(a => new { a.StartTime, a.Status, StaffName = a.Staff.FullName })
            .ToListAsync(ct);
        return list.Select(a => (object)new
        {
            startTime = a.StartTime,
            status    = a.Status,
            staffName = R.ShortName(a.StaffName),
        }).ToList();
    }

    private static object BookingContext(CustomerPackage p, PackageBookingState state) => new
    {
        token             = p.PublicToken,
        packageName       = p.Name,
        serviceId         = p.ServiceId,
        serviceName       = p.Service?.Name,
        customerFirstName = R.FirstName(p.Customer?.FullName),
        salonSubdomain    = p.Tenant?.Subdomain,
        totalSessions     = p.TotalSessions,
        usedSessions      = state.Used,
        remainingSessions = state.Remaining,
        bookableSessions  = state.Bookable,
        nextSessionNumber = state.NextNumber,
        expiresAt         = p.ExpiresAt,
        canBook           = state.CanBook,
        reason            = state.BlockReason,
    };

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

public record PackageLookupRequest(string? Subdomain, string? Phone);
