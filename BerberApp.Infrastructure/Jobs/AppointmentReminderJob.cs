using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Infrastructure.Jobs;

public class AppointmentReminderJob
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISmsService _smsService;
    private readonly string _frontendBase;

    public AppointmentReminderJob(
        IAppDbContext context,
        IWhatsAppService whatsAppService,
        ISmsService smsService,
        IConfiguration config)
    {
        _context      = context;
        _whatsAppService = whatsAppService;
        _smsService   = smsService;
        _frontendBase = config["AppSettings:FrontendBaseUrl"] ?? "https://berberapp.com.tr";
    }

    [AutomaticRetry(Attempts = 2)]
    public async Task SendRemindersAsync()
    {
        // Yarın sınırlarını Türkiye saatine göre hesapla (UTC+3).
        var turkeyTz      = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var nowTurkey     = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
        var tomorrowStart = new DateTime(nowTurkey.Year, nowTurkey.Month, nowTurkey.Day, 0, 0, 0).AddDays(1);
        var tomorrowEnd   = tomorrowStart.AddDays(1);
        var tomorrow      = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(tomorrowStart, DateTimeKind.Unspecified), turkeyTz);
        var dayAfter      = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(tomorrowEnd,   DateTimeKind.Unspecified), turkeyTz);

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
                var salonName = apt.Tenant?.Name ?? string.Empty;
                var mapsUrl   = !string.IsNullOrWhiteSpace(apt.Tenant?.Subdomain)
                    ? $"{_frontendBase}/map/{apt.Tenant.Subdomain}"
                    : string.Empty;

                if (apt.Tenant?.PreferredNotificationChannel == NotificationChannel.Sms)
                {
                    await _smsService.SendAppointmentReminderAsync(
                        apt.Customer.Phone, apt.Customer.FullName,
                        apt.Service.Name, apt.StartTime, salonName, mapsUrl);
                }
                else
                {
                    await _whatsAppService.SendAppointmentReminderAsync(
                        apt.Customer.Phone, apt.Customer.FullName,
                        apt.Service.Name, apt.StartTime, salonName, mapsUrl);
                }
            }
            catch { }
        }
    }
}
