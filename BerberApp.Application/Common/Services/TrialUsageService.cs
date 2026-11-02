using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Common.Services;

/// <summary>
/// İşletmenin deneme süresi boyunca sistemden fiilen ne aldığı.
///
/// Deneme bitiş uyarısında ve panel şeridinde soyut bir "planınızı yükseltin"
/// yerine işletmenin kendi sayıları gösteriliyor: aldığı randevu, gelen
/// müşteri, sistemin onun adına gönderdiği hatırlatma ve bu randevuların
/// cirosu. Kendi rakamını gören işletme neyi satın aldığını biliyor.
/// </summary>
public record TrialUsage(
    bool IsOnTrial,
    DateTime? StartedAt,
    DateTime? EndsAt,
    int DaysLeft,
    int CompletedAppointments,
    int DistinctCustomers,
    int NotificationsSent,
    decimal RevenueInTry
);

public interface ITrialUsageService
{
    Task<TrialUsage> GetAsync(Guid tenantId, CancellationToken ct = default);
}

public class TrialUsageService : ITrialUsageService
{
    private readonly IAppDbContext _context;
    private readonly IExchangeRateService _exchangeRates;

    public TrialUsageService(IAppDbContext context, IExchangeRateService exchangeRates)
    {
        _context = context;
        _exchangeRates = exchangeRates;
    }

    public async Task<TrialUsage> GetAsync(Guid tenantId, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;

        var trial = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId &&
                        s.Status == SubscriptionStatus.Trial &&
                        s.ExpiryDate > now)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync(ct);

        if (trial is null)
            return new TrialUsage(false, null, null, 0, 0, 0, 0, 0m);

        return await BuildAsync(tenantId, trial.StartDate, trial.ExpiryDate, true, ct);
    }

    /// <summary>
    /// Verilen aralık için kullanım özeti. Hatırlatma işi, aboneliği elinde
    /// olduğu için bu yolu doğrudan kullanıyor ve aynı sorguyu tekrar atmıyor.
    /// </summary>
    public async Task<TrialUsage> BuildAsync(
        Guid tenantId, DateTime since, DateTime endsAt, bool isOnTrial, CancellationToken ct = default)
    {
        var appointments = await _context.Appointments
            .Where(a => a.TenantId == tenantId &&
                        a.Status == AppointmentStatus.Completed &&
                        a.StartTime >= since)
            .Select(a => new { a.CustomerId, a.ServiceId, a.ActualTotalPrice })
            .ToListAsync(ct);

        var notifications = await _context.Notifications
            .Where(n => n.Appointment.TenantId == tenantId &&
                        n.IsSent && n.SentAt >= since)
            .CountAsync(ct);

        // Ciro: gelir raporuyla aynı hesap — gerçekleşen tutar varsa o, yoksa
        // hizmetin liste fiyatının TRY karşılığı.
        var services = await _context.Services
            .Where(s => s.TenantId == tenantId)
            .Select(s => new { s.Id, s.Price, s.Currency })
            .ToListAsync(ct);

        var rates = await _exchangeRates.GetRatesToTryAsync(
            services.Select(s => s.Currency).Distinct(), ct);

        decimal revenue = 0m;
        foreach (var appointment in appointments)
        {
            if (appointment.ActualTotalPrice.HasValue)
            {
                revenue += appointment.ActualTotalPrice.Value;
                continue;
            }

            var service = services.FirstOrDefault(s => s.Id == appointment.ServiceId);
            if (service is null) continue;
            revenue += (service.Price ?? 0) * rates.GetValueOrDefault(service.Currency.ToUpper(), 1m);
        }

        var daysLeft = Math.Max(0, (int)Math.Ceiling((endsAt - DateTime.UtcNow).TotalDays));

        return new TrialUsage(
            IsOnTrial:             isOnTrial,
            StartedAt:             since,
            EndsAt:                endsAt,
            DaysLeft:              daysLeft,
            CompletedAppointments: appointments.Count,
            DistinctCustomers:     appointments.Select(a => a.CustomerId).Distinct().Count(),
            NotificationsSent:     notifications,
            RevenueInTry:          Math.Round(revenue, 2));
    }
}
