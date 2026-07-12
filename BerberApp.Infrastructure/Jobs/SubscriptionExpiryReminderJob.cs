using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Infrastructure.Jobs;

public class SubscriptionExpiryReminderJob
{
    private readonly IAppDbContext _context;
    private readonly IEmailService _email;

    public SubscriptionExpiryReminderJob(IAppDbContext context, IEmailService email)
    {
        _context = context;
        _email   = email;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SendExpiryRemindersAsync()
    {
        var now = DateTime.UtcNow;

        // 7 gün kalan (6-8 gün) ve 1 gün kalan (12-36 saat) abonelikler
        await NotifyAsync(
            await LoadExpiringAsync(now.AddDays(6), now.AddDays(8)), 7);
        await NotifyAsync(
            await LoadExpiringAsync(now.AddHours(12), now.AddHours(36)), 1);
    }

    private async Task<List<Domain.Entities.Subscription>> LoadExpiringAsync(DateTime from, DateTime to)
        => await _context.Subscriptions
            .Include(s => s.Tenant)
            .Where(s => s.Status == SubscriptionStatus.Active &&
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

                await _email.SendSubscriptionExpiryWarningAsync(
                    admin.Email,
                    $"{admin.FirstName} {admin.LastName}".Trim(),
                    sub.Tenant!.Name,
                    daysLeft,
                    sub.ExpiryDate);
            }
            catch { /* tek bir bildirimin hatası job'u durdurmasın */ }
        }
    }
}
