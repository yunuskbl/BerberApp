using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Common.Services;

/// <summary>Bir paketin randevu açısından durumu.</summary>
/// <param name="Used">İşlenmiş (iptal edilmemiş) seans.</param>
/// <param name="Remaining">Pakette kalan seans.</param>
/// <param name="Reserved">Bekleyen/onaylı, henüz tamamlanmamış paket randevusu.</param>
/// <param name="Bookable">Daha alınabilecek randevu sayısı = kalan − ayrılmış.</param>
/// <param name="NextNumber">Yeni alınacak randevunun kaçıncı seans olacağı.</param>
/// <param name="BlockReason">Randevu alınamıyorsa nedeni; alınabiliyorsa null.</param>
public sealed record PackageBookingState(int Used, int Remaining, int Reserved, int Bookable, int NextNumber, string? BlockReason)
{
    public bool CanBook => BlockReason is null;
}

/// <summary>
/// Paketten randevu kuralları.
///
/// Müşteri paket takip bağlantısından (ya da telefonunu doğruladıktan sonra)
/// paketinden randevu alır. Randevu paketle bağlanır, ücret alınmaz; randevu
/// tamamlanınca seans pakete kendiliğinden işlenir — personel aynı seansı ayrıca
/// girmez. Kalan seanstan fazla ileri randevu alınamaz.
/// </summary>
public static class PackageBooking
{
    private static readonly AppointmentStatus[] OpenStatuses = [AppointmentStatus.Pending, AppointmentStatus.Confirmed];

    public static async Task<PackageBookingState> GetStateAsync(IAppDbContext db, CustomerPackage p, CancellationToken ct = default)
    {
        var used = await db.PackageSessions.CountAsync(s => s.CustomerPackageId == p.Id && !s.IsVoid, ct);
        var reserved = await db.Appointments.CountAsync(a => a.CustomerPackageId == p.Id && OpenStatuses.Contains(a.Status), ct);

        var remaining = Math.Max(0, p.TotalSessions - used);
        var bookable  = Math.Max(0, remaining - reserved);

        string? reason =
            p.Status == CustomerPackageStatus.Cancelled ? "Bu paket salon tarafından kapatıldı." :
            remaining == 0                              ? "Paketinizin tüm seansları kullanıldı." :
            SessionPackageRules.IsExpired(p.Status, p.ExpiresAt, DateTime.UtcNow)
                                                        ? "Paketinizin geçerlilik süresi doldu." :
            bookable == 0                               ? "Kalan seanslarınızın hepsi için zaten randevunuz var." :
            null;

        return new PackageBookingState(used, remaining, reserved, bookable, used + reserved + 1, reason);
    }

    /// <summary>
    /// Paketten alınan randevu tamamlandı: seansı pakete işler.
    ///
    /// Personel aynı gün seansı paket ekranından elle de işlemiş olabilir; o
    /// durumda yeni seans açılmaz, elle işlenen seans bu randevuya bağlanır —
    /// aynı seans iki kez düşmesin. Paket iptal edildiyse ya da kalan seans
    /// yoksa hiçbir şey yapılmaz ve null döner.
    /// </summary>
    public static async Task<PackageSession?> RecordFromAppointmentAsync(
        IAppDbContext db, AppointmentEntity appointment, DateTime nowUtc, CancellationToken ct = default)
    {
        if (appointment.CustomerPackageId is not { } packageId) return null;

        var pkg = await db.CustomerPackages
            .Include(p => p.Sessions)
            .FirstOrDefaultAsync(p => p.Id == packageId && p.TenantId == appointment.TenantId, ct);
        if (pkg is null || pkg.Status == CustomerPackageStatus.Cancelled) return null;

        var linked = pkg.Sessions.FirstOrDefault(s => s.AppointmentId == appointment.Id && !s.IsVoid);
        if (linked is not null) return linked;

        var day = IstanbulDate(appointment.StartTime);
        var manual = pkg.Sessions
            .Where(s => s.AppointmentId == null && !s.IsVoid && IstanbulDate(s.PerformedAt) == day)
            .OrderBy(s => s.PerformedAt)
            .FirstOrDefault();
        if (manual is not null)
        {
            manual.AppointmentId = appointment.Id;
            manual.StaffId ??= appointment.StaffId;
            return manual;
        }

        var used = SessionPackageRules.UsedSessions(pkg.Sessions);
        if (used >= pkg.TotalSessions) return null;

        var session = new PackageSession
        {
            TenantId             = pkg.TenantId,
            CustomerPackageId    = pkg.Id,
            StaffId              = appointment.StaffId,
            AppointmentId        = appointment.Id,
            PerformedAt          = appointment.StartTime,
            ConfirmCode          = SessionPackageRules.NewToken(),
            ConfirmCodeExpiresAt = nowUtc + SessionPackageRules.ConfirmCodeLifetime,
        };
        db.PackageSessions.Add(session);
        pkg.Sessions.Add(session);
        pkg.Status = SessionPackageRules.StatusFor(pkg.Status, pkg.TotalSessions, used + 1);
        return session;
    }

    private static readonly TimeZoneInfo Istanbul = FindIstanbul();

    private static DateOnly IstanbulDate(DateTime utc)
        => DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Istanbul));

    private static TimeZoneInfo FindIstanbul()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
    }
}
