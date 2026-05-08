using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Infrastructure.Jobs;

public class AppointmentReminderJob
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISmsService _smsService;

    public AppointmentReminderJob(IAppDbContext context, IWhatsAppService whatsAppService, ISmsService smsService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _smsService = smsService;
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SendRemindersAsync()
    {
        var tomorrow = DateTime.UtcNow.Date.AddDays(1);
        var dayAfter = tomorrow.AddDays(1);

        var appointments = await _context.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Service)
            .Include(x => x.Tenant)
            .Where(x => x.StartTime >= tomorrow &&
                        x.StartTime < dayAfter &&
                        x.Status == AppointmentStatus.Confirmed)
            .ToListAsync();

        foreach (var apt in appointments)
        {
            try
            {
                var salonName = apt.Tenant?.Name    ?? string.Empty;
                var address   = apt.Tenant?.Address ?? string.Empty;

                if (apt.Tenant?.PreferredNotificationChannel == NotificationChannel.Sms)
                {
                    await _smsService.SendAppointmentReminderAsync(
                        apt.Customer.Phone,
                        apt.Customer.FullName,
                        apt.Service.Name,
                        apt.StartTime,
                        salonName,
                        address
                    );
                }
                else
                {
                    await _whatsAppService.SendAppointmentReminderAsync(
                        apt.Customer.Phone,
                        apt.Customer.FullName,
                        apt.Service.Name,
                        apt.StartTime,
                        salonName,
                        address
                    );
                }
            }
            catch { }
        }
    }
}
