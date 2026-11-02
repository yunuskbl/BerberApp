using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Services;
using BerberApp.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Infrastructure.Jobs;

public class SubscriptionExpiryReminderJob
{
    private readonly IAppDbContext _context;
    private readonly IEmailService _email;
    private readonly TrialUsageService _usage;

    public SubscriptionExpiryReminderJob(
        IAppDbContext context, IEmailService email, TrialUsageService usage)
    {
        _context = context;
        _email   = email;
        _usage   = usage;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SendExpiryRemindersAsync()
    {
        var now = DateTime.UtcNow;

        // 7 / 3 / 1 gün kala. Üç basamak var çünkü tek bir uyarı, kararın
        // verildiği anı yakalamıyor: yedi gün erken, bir gün geç.
        await NotifyAsync(await LoadExpiringAsync(now.AddDays(6),  now.AddDays(8)),  7);
        await NotifyAsync(await LoadExpiringAsync(now.AddDays(2),  now.AddDays(4)),  3);
        await NotifyAsync(await LoadExpiringAsync(now.AddHours(12), now.AddHours(36)), 1);
    }

    /// <summary>
    /// Süresi dolmak üzere olan abonelikler. <b>Deneme abonelikleri de dahil</b>:
    /// sorgu yalnızca Active durumunu arıyordu ve deneme kayıtları Trial
    /// durumunda olduğu için bugüne kadar tek bir deneme bitiş uyarısı
    /// gönderilmemişti — yani hatırlatma, en çok gerektiği yerde hiç çalışmıyordu.
    /// </summary>
    private async Task<List<Domain.Entities.Subscription>> LoadExpiringAsync(DateTime from, DateTime to)
        => await _context.Subscriptions
            .Include(s => s.Tenant)
            .Where(s => (s.Status == SubscriptionStatus.Active ||
                         s.Status == SubscriptionStatus.Trial) &&
                        s.ExpiryDate >= from && s.ExpiryDate <= to &&
                        s.Tenant != null)
            .ToListAsync();

    private async Task NotifyAsync(List<Domain.Entities.Subscription> subs, int daysLeft)
    {
        foreach (var sub in subs)
        {
            try
            {
                // İşletme sahibinin (Admin) e-postasını bul
                var admin = await _context.Users
                    .Where(u => u.TenantId == sub.TenantId && u.Role == UserRole.Admin)
                    .Select(u => new { u.Email, u.FirstName, u.LastName })
                    .FirstOrDefaultAsync();

                if (admin is null || string.IsNullOrWhiteSpace(admin.Email)) continue;

                var isTrial = sub.Status == SubscriptionStatus.Trial;

                // Dönem boyunca ne kullandığı. "Planınızı yükseltin" soyut bir
                // çağrı; "34 randevu aldın, 12 müşteri geri geldi" işletmenin
                // kendi kanıtı.
                var usage = await _usage.BuildAsync(
                    sub.TenantId, sub.StartDate, sub.ExpiryDate, isTrial);

                await _email.SendSubscriptionExpiryWarningAsync(
                    admin.Email,
                    $"{admin.FirstName} {admin.LastName}".Trim(),
                    sub.Tenant!.Name,
                    daysLeft,
                    sub.ExpiryDate,
                    isTrial,
                    usage.CompletedAppointments,
                    usage.DistinctCustomers,
                    usage.NotificationsSent,
                    usage.RevenueInTry);
            }
            catch { /* tek bir bildirimin hatası job'u durdurmasın */ }
        }
    }
}
