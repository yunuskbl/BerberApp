using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Infrastructure.Jobs;

public class SubscriptionExpiryReminderJob
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;

    public SubscriptionExpiryReminderJob(IAppDbContext context, IWhatsAppService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SendExpiryRemindersAsync()
    {
        var now = DateTime.UtcNow;
        var in7Days = now.AddDays(7);
        var in1Day = now.AddDays(1);

        // 7 gün kalan abonelikler (6-8 gün arasında)
        var expiring7 = await _context.Subscriptions
            .Include(s => s.Tenant)
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.ExpiryDate >= now.AddDays(6) &&
                        s.ExpiryDate <= now.AddDays(8) &&
                        s.Tenant != null &&
                        s.Tenant.NotificationPhone != null)
            .ToListAsync();

        foreach (var sub in expiring7)
        {
            try
            {
                await _whatsAppService.SendSubscriptionExpiryWarningAsync(
                    sub.Tenant!.NotificationPhone!, sub.Tenant.Name, 7);
            }
            catch { }
        }

        // 1 gün kalan abonelikler (12-36 saat arasında)
        var expiring1 = await _context.Subscriptions
            .Include(s => s.Tenant)
            .Where(s => s.Status == SubscriptionStatus.Active &&
                        s.ExpiryDate >= now.AddHours(12) &&
                        s.ExpiryDate <= now.AddHours(36) &&
                        s.Tenant != null &&
                        s.Tenant.NotificationPhone != null)
            .ToListAsync();

        foreach (var sub in expiring1)
        {
            try
            {
                await _whatsAppService.SendSubscriptionExpiryWarningAsync(
                    sub.Tenant!.NotificationPhone!, sub.Tenant.Name, 1);
            }
            catch { }
        }
    }
}
