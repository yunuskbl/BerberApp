using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Infrastructure.Jobs;

/// <summary>
/// Her 15 dakikada bir çalışır. StartTime'a 1 saat (± 15 dakika) kalmış,
/// henüz 1h hatırlatması gönderilmemiş onaylı randevulara "1 saat sonra" mesajı gönderir.
/// </summary>
public class AppointmentReminder1hJob
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISmsService _smsService;
    private readonly string _frontendBase;

    public AppointmentReminder1hJob(
        IAppDbContext context,
        IWhatsAppService whatsAppService,
        ISmsService smsService,
        IConfiguration config)
    {
        _context         = context;
        _whatsAppService = whatsAppService;
        _smsService      = smsService;
        _frontendBase    = config["AppSettings:FrontendBaseUrl"] ?? "https://ayarliyo.com";
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SendRemindersAsync()
    {
        var now         = DateTime.UtcNow;
        var windowStart = now.AddMinutes(45);  // 1h - 15 dk tolerans
        var windowEnd   = now.AddMinutes(75);  // 1h + 15 dk tolerans

        var appointments = await _context.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Service)
            .Include(x => x.Tenant)
            .Where(x => x.StartTime >= windowStart &&
                        x.StartTime <  windowEnd &&
                        x.Status == AppointmentStatus.Confirmed &&
                        !x.ReminderSent1h)
            .ToListAsync();

        foreach (var apt in appointments)
        {
            try
            {
                var salonName  = apt.Tenant?.Name ?? string.Empty;
                var mapsUrl    = !string.IsNullOrWhiteSpace(apt.Tenant?.Subdomain)
                    ? $"{_frontendBase}/api/map/{apt.Tenant.Subdomain}"
                    : string.Empty;
                var bookingUrl = !string.IsNullOrWhiteSpace(apt.Tenant?.Subdomain)
                    ? $"{_frontendBase}/{apt.Tenant.Subdomain}"
                    : string.Empty;

                if (apt.Tenant?.PreferredNotificationChannel == NotificationChannel.Sms)
                {
                    await _smsService.SendAppointmentReminder1hAsync(
                        apt.Customer.Phone, apt.Customer.FullName,
                        apt.Service.Name, apt.StartTime, salonName, mapsUrl, bookingUrl);
                }
                else
                {
                    await _whatsAppService.SendAppointmentReminder1hAsync(
                        apt.Customer.Phone, apt.Customer.FullName,
                        apt.Service.Name, apt.StartTime, salonName, mapsUrl, bookingUrl);
                }

                // Gönderildi olarak işaretle
                apt.ReminderSent1h = true;
            }
            catch { }
        }

        if (appointments.Any(a => a.ReminderSent1h))
            await _context.SaveChangesAsync(CancellationToken.None);
    }
}
