using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Services;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using R = BerberApp.Application.Common.Services.SessionPackageRules;

namespace BerberApp.Api.Controllers;

/// <summary>
/// Seans paketleri — işletme tarafı.
///
/// Akış: işletme paket tanımlar (ör. "Lazer — 8 seans"), müşteriye satar,
/// her gelişte seans işler. Seans işlendiği anda düşer; işlenen seans için
/// tek kullanımlık bir onay kodu üretilir ve panel bunu QR olarak gösterir.
/// Müşteri QR'ı okutursa seans "müşteri onayladı" olarak işaretlenir
/// (PublicPackagesController). Ödemeler pakete girilir, kalan borç iki
/// tarafta da görünür. Yanlış seans/ödeme silinmez, iptal edilir.
/// </summary>
[Authorize(Roles = "Admin,SuperAdmin")]
public class PackagesController : BaseApiController
{
    private readonly IAppDbContext _context;

    public PackagesController(IMediator mediator, IAppDbContext context) : base(mediator)
    {
        _context = context;
    }

    // ═══ Paket tanımları ═════════════════════════════════════════════════════

    [HttpGet("templates")]
    public async Task<IActionResult> GetTemplates(CancellationToken ct)
    {
        var list = await _context.PackageTemplates
            .AsNoTracking()
            .Where(t => t.TenantId == TenantId)
            .OrderByDescending(t => t.IsActive).ThenBy(t => t.Name)
            .Select(t => new PackageTemplateDto(
                t.Id, t.Name, t.ServiceId, t.Service != null ? t.Service.Name : null,
                t.SessionCount, t.Price, t.Currency, t.ValidityDays, t.IsActive))
            .ToListAsync(ct);
        return Success(list);
    }

    [HttpPost("templates")]
    public async Task<IActionResult> CreateTemplate([FromBody] PackageTemplateRequest req, CancellationToken ct)
    {
        var error = await ValidateTemplateAsync(req, ct);
        if (error is not null) return BadRequest(new { success = false, message = error });

        var t = new PackageTemplate { TenantId = TenantId };
        Apply(t, req);
        _context.PackageTemplates.Add(t);
        await _context.SaveChangesAsync(ct);
        return Created(new { t.Id }, "Paket tanımı eklendi.");
    }

    [HttpPut("templates/{id:guid}")]
    public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] PackageTemplateRequest req, CancellationToken ct)
    {
        var t = await _context.PackageTemplates.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (t is null) return NotFound(new { success = false, message = "Paket tanımı bulunamadı." });

        var error = await ValidateTemplateAsync(req, ct);
        if (error is not null) return BadRequest(new { success = false, message = error });

        // Satılmış paketler satış anındaki değerleri taşıyor; burada değişen
        // yalnızca bundan sonraki satışlar.
        Apply(t, req);
        await _context.SaveChangesAsync(ct);
        return Success(new { t.Id }, "Paket tanımı güncellendi.");
    }

    [HttpDelete("templates/{id:guid}")]
    public async Task<IActionResult> DeleteTemplate(Guid id, CancellationToken ct)
    {
        var t = await _context.PackageTemplates.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (t is null) return NotFound(new { success = false, message = "Paket tanımı bulunamadı." });

        _context.PackageTemplates.Remove(t);   // yumuşak silme (AppDbContext)
        await _context.SaveChangesAsync(ct);
        return Success<object?>(null, "Paket tanımı silindi.");
    }

    private async Task<string?> ValidateTemplateAsync(PackageTemplateRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return "Paket adı gerekli.";
        if (req.Name.Trim().Length > R.MaxNameLength) return $"Paket adı en fazla {R.MaxNameLength} karakter olabilir.";
        if (req.SessionCount < 1 || req.SessionCount > R.MaxSessions) return $"Seans sayısı 1 ile {R.MaxSessions} arasında olmalı.";
        if (req.Price < 0 || req.Price > R.MaxPrice) return "Fiyat geçersiz.";
        if (req.ValidityDays is { } d && (d < 1 || d > R.MaxValidityDays)) return $"Geçerlilik 1 ile {R.MaxValidityDays} gün arasında olmalı.";
        if (req.ServiceId is { } sid &&
            !await _context.Services.AnyAsync(s => s.Id == sid && s.TenantId == TenantId, ct))
            return "Hizmet bulunamadı.";
        return null;
    }

    private static void Apply(PackageTemplate t, PackageTemplateRequest req)
    {
        t.Name         = req.Name!.Trim();
        t.ServiceId    = req.ServiceId;
        t.SessionCount = req.SessionCount;
        t.Price        = Math.Round(req.Price, 2, MidpointRounding.AwayFromZero);
        t.ValidityDays = req.ValidityDays;
        t.IsActive     = req.IsActive;
    }

    // ═══ Satılan paketler ════════════════════════════════════════════════════

    /// <param name="status">active (varsayılan), completed, cancelled, all</param>
    [HttpGet]
    public async Task<IActionResult> GetPackages(
        [FromQuery] string? status, [FromQuery] Guid? customerId, CancellationToken ct)
    {
        var query = PackagesWithDetails().Where(p => p.TenantId == TenantId);

        if (customerId is { } cid) query = query.Where(p => p.CustomerId == cid);

        query = (status ?? "active").ToLowerInvariant() switch
        {
            "completed" => query.Where(p => p.Status == CustomerPackageStatus.Completed),
            "cancelled" => query.Where(p => p.Status == CustomerPackageStatus.Cancelled),
            "all"       => query,
            _           => query.Where(p => p.Status == CustomerPackageStatus.Active),
        };

        var packages = await query.AsNoTracking().ToListAsync(ct);
        var now = DateTime.UtcNow;
        var list = packages
            .Select(p => ToSummary(p, now))
            .OrderByDescending(s => s.LastActivityAt)
            .ToList();
        return Success(list);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetPackage(Guid id, CancellationToken ct)
    {
        var p = await PackagesWithDetails().AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId, ct);
        if (p is null) return NotFound(new { success = false, message = "Paket bulunamadı." });
        return Success(ToDetail(p, DateTime.UtcNow, await UpcomingAsync(p.Id, ct)));
    }

    [HttpPost]
    public async Task<IActionResult> SellPackage([FromBody] SellPackageRequest req, CancellationToken ct)
    {
        var customer = await _context.Customers.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == req.CustomerId && c.TenantId == TenantId, ct);
        if (customer is null) return BadRequest(new { success = false, message = "Müşteri bulunamadı." });

        PackageTemplate? template = null;
        if (req.TemplateId is { } tid)
        {
            template = await _context.PackageTemplates.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tid && t.TenantId == TenantId, ct);
            if (template is null) return BadRequest(new { success = false, message = "Paket tanımı bulunamadı." });
        }

        // Tanımdan gelen değerler; satışta açıkça girilen (ör. indirimli fiyat) üstüne yazar.
        var name     = (req.Name ?? template?.Name)?.Trim();
        var sessions = req.TotalSessions ?? template?.SessionCount ?? 0;
        var price    = req.Price ?? template?.Price;
        var validity = req.ValidityDays ?? template?.ValidityDays;
        var service  = req.ServiceId ?? template?.ServiceId;

        if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { success = false, message = "Paket adı gerekli." });
        if (name.Length > R.MaxNameLength) return BadRequest(new { success = false, message = $"Paket adı en fazla {R.MaxNameLength} karakter olabilir." });
        if (sessions < 1 || sessions > R.MaxSessions) return BadRequest(new { success = false, message = $"Seans sayısı 1 ile {R.MaxSessions} arasında olmalı." });
        if (price is null || price < 0 || price > R.MaxPrice) return BadRequest(new { success = false, message = "Paket fiyatı geçersiz." });
        if (validity is { } vd && (vd < 1 || vd > R.MaxValidityDays)) return BadRequest(new { success = false, message = $"Geçerlilik 1 ile {R.MaxValidityDays} gün arasında olmalı." });
        if (req.Note is { Length: > R.MaxNoteLength }) return BadRequest(new { success = false, message = $"Not en fazla {R.MaxNoteLength} karakter olabilir." });
        if (service is { } sid && !await _context.Services.AnyAsync(s => s.Id == sid && s.TenantId == TenantId, ct))
            return BadRequest(new { success = false, message = "Hizmet bulunamadı." });

        var priceRounded = Math.Round(price.Value, 2, MidpointRounding.AwayFromZero);
        if (req.InitialPayment is { } ip)
        {
            var err = ValidatePayment(ip, remainingDebt: priceRounded);
            if (err is not null) return BadRequest(new { success = false, message = err });
        }

        var started = req.StartedAt ?? DateTime.UtcNow;
        var pkg = new CustomerPackage
        {
            TenantId          = TenantId,
            CustomerId        = customer.Id,
            PackageTemplateId = template?.Id,
            ServiceId         = service,
            Name              = name,
            TotalSessions     = sessions,
            Price             = priceRounded,
            Currency          = template?.Currency ?? "TRY",
            StartedAt         = started,
            ExpiresAt         = validity is { } days ? started.AddDays(days) : null,
            Status            = CustomerPackageStatus.Active,
            PublicToken       = R.NewToken(),
            Note              = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim(),
        };
        _context.CustomerPackages.Add(pkg);

        if (req.InitialPayment is { } first)
            _context.PackagePayments.Add(NewPayment(pkg, first));

        await _context.SaveChangesAsync(ct);
        return await DetailResponse(pkg.Id, "Paket satıldı.", ct, created: true);
    }

    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> CancelPackage(Guid id, [FromBody] ReasonRequest? req, CancellationToken ct)
    {
        var pkg = await _context.CustomerPackages.FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId, ct);
        if (pkg is null) return NotFound(new { success = false, message = "Paket bulunamadı." });
        if (pkg.Status == CustomerPackageStatus.Cancelled) return BadRequest(new { success = false, message = "Paket zaten iptal edilmiş." });
        if (req?.Reason is { Length: > R.MaxReasonLength }) return BadRequest(new { success = false, message = $"Açıklama en fazla {R.MaxReasonLength} karakter olabilir." });

        pkg.Status       = CustomerPackageStatus.Cancelled;
        pkg.CancelledAt  = DateTime.UtcNow;
        pkg.CancelReason = string.IsNullOrWhiteSpace(req?.Reason) ? null : req.Reason.Trim();
        await _context.SaveChangesAsync(ct);
        return await DetailResponse(pkg.Id, "Paket iptal edildi.", ct);
    }

    // ═══ Seanslar ════════════════════════════════════════════════════════════

    /// <summary>
    /// Seans işler: seans hemen düşer ve müşterinin okutacağı tek kullanımlık
    /// onay kodu döner (panel bunu QR olarak gösterir).
    /// </summary>
    [HttpPost("{id:guid}/sessions")]
    public async Task<IActionResult> RecordSession(Guid id, [FromBody] RecordSessionRequest? req, CancellationToken ct)
    {
        var pkg = await _context.CustomerPackages
            .Include(p => p.Sessions)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId, ct);
        if (pkg is null) return NotFound(new { success = false, message = "Paket bulunamadı." });
        if (pkg.Status == CustomerPackageStatus.Cancelled)
            return BadRequest(new { success = false, message = "İptal edilmiş pakete seans işlenemez." });

        var used = R.UsedSessions(pkg.Sessions);
        if (used >= pkg.TotalSessions)
            return BadRequest(new { success = false, message = "Paketin tüm seansları kullanıldı." });

        if (req?.Note is { Length: > R.MaxNoteLength })
            return BadRequest(new { success = false, message = $"Not en fazla {R.MaxNoteLength} karakter olabilir." });
        if (req?.StaffId is { } staffId &&
            !await _context.Staff.AnyAsync(s => s.Id == staffId && s.TenantId == TenantId, ct))
            return BadRequest(new { success = false, message = "Personel bulunamadı." });

        var now = DateTime.UtcNow;
        var performedAt = req?.PerformedAt ?? now;
        if (performedAt > now.AddMinutes(10))
            return BadRequest(new { success = false, message = "Seans tarihi ileri bir zaman olamaz." });

        var session = new PackageSession
        {
            TenantId             = TenantId,
            CustomerPackageId    = pkg.Id,
            StaffId              = req?.StaffId,
            PerformedAt          = performedAt,
            Note                 = string.IsNullOrWhiteSpace(req?.Note) ? null : req!.Note!.Trim(),
            ConfirmCode          = R.NewToken(),
            ConfirmCodeExpiresAt = now + R.ConfirmCodeLifetime,
        };
        _context.PackageSessions.Add(session);
        pkg.Status = R.StatusFor(pkg.Status, pkg.TotalSessions, used + 1);

        await _context.SaveChangesAsync(ct);
        return await DetailResponse(pkg.Id, "Seans işlendi.", ct, created: true,
            confirm: new SessionConfirmDto(session.Id, session.ConfirmCode!, session.ConfirmCodeExpiresAt!.Value));
    }

    /// <summary>Müşteri onayı bekleyen seansın durumu — panel QR açıkken bunu sorar.</summary>
    [HttpGet("{id:guid}/sessions/{sessionId:guid}")]
    public async Task<IActionResult> GetSessionStatus(Guid id, Guid sessionId, CancellationToken ct)
    {
        var s = await _context.PackageSessions.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.CustomerPackageId == id && x.TenantId == TenantId, ct);
        if (s is null) return NotFound(new { success = false, message = "Seans bulunamadı." });
        return Success(new { s.Id, s.CustomerConfirmedAt, s.IsVoid, s.ConfirmCodeExpiresAt });
    }

    /// <summary>Süresi dolan onay kodunun yerine yenisini üretir.</summary>
    [HttpPost("{id:guid}/sessions/{sessionId:guid}/confirm-code")]
    public async Task<IActionResult> RenewConfirmCode(Guid id, Guid sessionId, CancellationToken ct)
    {
        var s = await _context.PackageSessions
            .FirstOrDefaultAsync(x => x.Id == sessionId && x.CustomerPackageId == id && x.TenantId == TenantId, ct);
        if (s is null) return NotFound(new { success = false, message = "Seans bulunamadı." });
        if (s.IsVoid) return BadRequest(new { success = false, message = "İptal edilmiş seans onaylanamaz." });
        if (s.CustomerConfirmedAt is not null) return BadRequest(new { success = false, message = "Müşteri bu seansı zaten onayladı." });

        s.ConfirmCode          = R.NewToken();
        s.ConfirmCodeExpiresAt = DateTime.UtcNow + R.ConfirmCodeLifetime;
        await _context.SaveChangesAsync(ct);
        return Success(new SessionConfirmDto(s.Id, s.ConfirmCode, s.ConfirmCodeExpiresAt.Value));
    }

    [HttpPost("{id:guid}/sessions/{sessionId:guid}/void")]
    public async Task<IActionResult> VoidSession(Guid id, Guid sessionId, [FromBody] ReasonRequest? req, CancellationToken ct)
    {
        var pkg = await _context.CustomerPackages
            .Include(p => p.Sessions)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId, ct);
        var s = pkg?.Sessions.FirstOrDefault(x => x.Id == sessionId);
        if (pkg is null || s is null) return NotFound(new { success = false, message = "Seans bulunamadı." });
        if (s.IsVoid) return BadRequest(new { success = false, message = "Seans zaten iptal edilmiş." });
        if (req?.Reason is { Length: > R.MaxReasonLength }) return BadRequest(new { success = false, message = $"Açıklama en fazla {R.MaxReasonLength} karakter olabilir." });

        s.IsVoid      = true;
        s.VoidedAt    = DateTime.UtcNow;
        s.VoidReason  = string.IsNullOrWhiteSpace(req?.Reason) ? null : req.Reason.Trim();
        s.ConfirmCode = null;   // iptal edilen seans artık onaylanamaz
        pkg.Status    = R.StatusFor(pkg.Status, pkg.TotalSessions, R.UsedSessions(pkg.Sessions));

        await _context.SaveChangesAsync(ct);
        return await DetailResponse(pkg.Id, "Seans iptal edildi; paket bakiyesine geri eklendi.", ct);
    }

    // ═══ Ödemeler ════════════════════════════════════════════════════════════

    [HttpPost("{id:guid}/payments")]
    public async Task<IActionResult> AddPayment(Guid id, [FromBody] PackagePaymentRequest req, CancellationToken ct)
    {
        var pkg = await _context.CustomerPackages
            .Include(p => p.Payments)
            .FirstOrDefaultAsync(p => p.Id == id && p.TenantId == TenantId, ct);
        if (pkg is null) return NotFound(new { success = false, message = "Paket bulunamadı." });

        var remaining = R.RemainingDebt(pkg.Price, R.Paid(pkg.Payments));
        var err = ValidatePayment(req, remaining);
        if (err is not null) return BadRequest(new { success = false, message = err });

        _context.PackagePayments.Add(NewPayment(pkg, req));
        await _context.SaveChangesAsync(ct);
        return await DetailResponse(pkg.Id, "Ödeme kaydedildi.", ct, created: true);
    }

    [HttpPost("{id:guid}/payments/{paymentId:guid}/void")]
    public async Task<IActionResult> VoidPayment(Guid id, Guid paymentId, CancellationToken ct)
    {
        var p = await _context.PackagePayments
            .FirstOrDefaultAsync(x => x.Id == paymentId && x.CustomerPackageId == id && x.TenantId == TenantId, ct);
        if (p is null) return NotFound(new { success = false, message = "Ödeme bulunamadı." });
        if (p.IsVoid) return BadRequest(new { success = false, message = "Ödeme zaten iptal edilmiş." });

        p.IsVoid   = true;
        p.VoidedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);
        return await DetailResponse(id, "Ödeme iptal edildi.", ct);
    }

    private static string? ValidatePayment(PackagePaymentRequest req, decimal remainingDebt)
    {
        if (req.Amount <= 0) return "Ödeme tutarı sıfırdan büyük olmalı.";
        if (Math.Round(req.Amount, 2, MidpointRounding.AwayFromZero) > remainingDebt)
            return remainingDebt == 0
                ? "Bu paketin ödenecek borcu yok."
                : $"Ödeme kalan borçtan ({remainingDebt:0.##} ₺) fazla olamaz.";
        if (req.Note is { Length: > R.MaxPaymentNote }) return $"Ödeme notu en fazla {R.MaxPaymentNote} karakter olabilir.";
        if (!Enum.IsDefined(req.Method)) return "Ödeme şekli geçersiz.";
        if (req.PaidAt is { } at && at > DateTime.UtcNow.AddMinutes(10)) return "Ödeme tarihi ileri bir zaman olamaz.";
        return null;
    }

    private PackagePayment NewPayment(CustomerPackage pkg, PackagePaymentRequest req) => new()
    {
        TenantId          = TenantId,
        CustomerPackageId = pkg.Id,
        Amount            = Math.Round(req.Amount, 2, MidpointRounding.AwayFromZero),
        Currency          = pkg.Currency,
        Method            = req.Method,
        PaidAt            = req.PaidAt ?? DateTime.UtcNow,
        Note              = string.IsNullOrWhiteSpace(req.Note) ? null : req.Note.Trim(),
    };

    // ═══ Yanıt şekilleri ═════════════════════════════════════════════════════

    private IQueryable<CustomerPackage> PackagesWithDetails() => _context.CustomerPackages
        .Include(p => p.Customer)
        .Include(p => p.Service)
        .Include(p => p.Sessions).ThenInclude(s => s.Staff)
        .Include(p => p.Payments);

    private async Task<IActionResult> DetailResponse(
        Guid id, string message, CancellationToken ct, bool created = false, SessionConfirmDto? confirm = null)
    {
        var p = await PackagesWithDetails().AsNoTracking().FirstAsync(x => x.Id == id, ct);
        var detail = ToDetail(p, DateTime.UtcNow, await UpcomingAsync(id, ct)) with { Confirm = confirm };
        return created ? Created(detail, message) : Success(detail, message);
    }

    internal static PackageSummaryDto ToSummary(CustomerPackage p, DateTime now)
    {
        var used = R.UsedSessions(p.Sessions);
        var paid = R.Paid(p.Payments);
        var lastSession = p.Sessions.Where(s => !s.IsVoid).Select(s => (DateTime?)s.PerformedAt).Max();
        var lastPayment = p.Payments.Where(x => !x.IsVoid).Select(x => (DateTime?)x.PaidAt).Max();
        var lastActivity = new[] { p.StartedAt, lastSession ?? DateTime.MinValue, lastPayment ?? DateTime.MinValue }.Max();

        return new PackageSummaryDto(
            p.Id, p.CustomerId, p.Customer?.FullName ?? string.Empty, p.Customer?.Phone,
            p.Name, p.ServiceId, p.Service?.Name,
            p.TotalSessions, used, Math.Max(0, p.TotalSessions - used),
            p.Price, paid, R.RemainingDebt(p.Price, paid), p.Currency,
            p.Status, R.IsExpired(p.Status, p.ExpiresAt, now),
            p.StartedAt, p.ExpiresAt, lastSession, lastActivity,
            p.PublicToken, p.Note);
    }

    /// <summary>Paketten alınmış, henüz tamamlanmamış randevular.</summary>
    private async Task<List<PackageAppointmentDto>> UpcomingAsync(Guid packageId, CancellationToken ct)
        => await _context.Appointments.AsNoTracking()
            .Where(a => a.CustomerPackageId == packageId && a.TenantId == TenantId &&
                        (a.Status == AppointmentStatus.Pending || a.Status == AppointmentStatus.Confirmed))
            .OrderBy(a => a.StartTime)
            .Select(a => new PackageAppointmentDto(a.Id, a.StartTime, a.Staff.FullName, a.Status))
            .ToListAsync(ct);

    internal static PackageDetailDto ToDetail(CustomerPackage p, DateTime now, IReadOnlyList<PackageAppointmentDto> upcoming)
    {
        var sessions = R.Numbered(p.Sessions)
            .Select(x => new PackageSessionDto(
                x.Session.Id, x.Number, x.Session.PerformedAt,
                x.Session.StaffId, x.Session.Staff?.FullName, x.Session.Note,
                x.Session.CustomerConfirmedAt, x.Session.IsVoid, x.Session.VoidedAt, x.Session.VoidReason,
                x.Session.AppointmentId))
            .Reverse()   // en yeni üstte
            .ToList();

        var payments = p.Payments
            .OrderByDescending(x => x.PaidAt).ThenByDescending(x => x.CreatedAt)
            .Select(x => new PackagePaymentDto(x.Id, x.Amount, x.PaidAt, x.Method, x.Note, x.IsVoid, x.VoidedAt))
            .ToList();

        return new PackageDetailDto(ToSummary(p, now), sessions, payments, upcoming, p.CancelledAt, p.CancelReason, null);
    }
}

// ─── İstekler ────────────────────────────────────────────────────────────────

public record PackageTemplateRequest(
    string? Name, Guid? ServiceId, int SessionCount, decimal Price, int? ValidityDays, bool IsActive = true);

public record SellPackageRequest(
    Guid CustomerId, Guid? TemplateId, string? Name, Guid? ServiceId, int? TotalSessions,
    decimal? Price, int? ValidityDays, DateTime? StartedAt, string? Note, PackagePaymentRequest? InitialPayment);

public record PackagePaymentRequest(
    decimal Amount, PackagePaymentMethod Method = PackagePaymentMethod.Cash, DateTime? PaidAt = null, string? Note = null);

public record RecordSessionRequest(Guid? StaffId, string? Note, DateTime? PerformedAt);

public record ReasonRequest(string? Reason);

// ─── Yanıtlar ────────────────────────────────────────────────────────────────

public record PackageTemplateDto(
    Guid Id, string Name, Guid? ServiceId, string? ServiceName,
    int SessionCount, decimal Price, string Currency, int? ValidityDays, bool IsActive);

public record PackageSummaryDto(
    Guid Id, Guid CustomerId, string CustomerName, string? CustomerPhone,
    string Name, Guid? ServiceId, string? ServiceName,
    int TotalSessions, int UsedSessions, int RemainingSessions,
    decimal Price, decimal Paid, decimal RemainingDebt, string Currency,
    CustomerPackageStatus Status, bool IsExpired,
    DateTime StartedAt, DateTime? ExpiresAt, DateTime? LastSessionAt, DateTime LastActivityAt,
    string PublicToken, string? Note);

public record PackageSessionDto(
    Guid Id, int? Number, DateTime PerformedAt, Guid? StaffId, string? StaffName, string? Note,
    DateTime? CustomerConfirmedAt, bool IsVoid, DateTime? VoidedAt, string? VoidReason,
    Guid? AppointmentId);

/// <summary>Paketten alınmış, henüz tamamlanmamış randevu.</summary>
public record PackageAppointmentDto(Guid Id, DateTime StartTime, string? StaffName, AppointmentStatus Status);

public record PackagePaymentDto(
    Guid Id, decimal Amount, DateTime PaidAt, PackagePaymentMethod Method, string? Note, bool IsVoid, DateTime? VoidedAt);

/// <summary>Seans işlendiğinde müşterinin okutacağı onay kodu.</summary>
public record SessionConfirmDto(Guid SessionId, string Code, DateTime ExpiresAt);

public record PackageDetailDto(
    PackageSummaryDto Package,
    IReadOnlyList<PackageSessionDto> Sessions,
    IReadOnlyList<PackagePaymentDto> Payments,
    IReadOnlyList<PackageAppointmentDto> Upcoming,
    DateTime? CancelledAt, string? CancelReason,
    SessionConfirmDto? Confirm);
