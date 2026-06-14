using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Services;

public class LinkNotificationService : INotificationService
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISmsService _smsService;
    private readonly ILogger<LinkNotificationService> _logger;
    private readonly string _frontendBase;

    public LinkNotificationService(
        IAppDbContext context,
        IWhatsAppService whatsAppService,
        ISmsService smsService,
        ILogger<LinkNotificationService> logger,
        IConfiguration config)
    {
        _context        = context;
        _whatsAppService = whatsAppService;
        _smsService     = smsService;
        _logger         = logger;
        _frontendBase   = config["AppSettings:FrontendBaseUrl"] ?? "https://ayarliyo.com";
    }

    public async Task SendAppointmentReceivedAsync(string recipient, AppointmentStatusDto dto)
    {
        _logger.LogInformation("[BİLDİRİM] Randevu alındı: TenantId={TenantId}, Alıcı={Recipient}", dto.TenantId, recipient);
        await Task.CompletedTask;
    }

    public async Task SendAppointmentConfirmedAsync(string recipient, AppointmentStatusDto dto)
    {
        try
        {
            var (channel, salonName, mapsUrl, bookingUrl, subdomain) = await GetTenantInfoAsync(dto.TenantId);
            var encodedPhone   = Uri.EscapeDataString(recipient);
            var appointmentsUrl = !string.IsNullOrWhiteSpace(subdomain)
                ? $"{_frontendBase}/randevularim/{subdomain}?phone={encodedPhone}"
                : string.Empty;

            if (channel == NotificationChannel.Sms)
            {
                await _smsService.SendAppointmentConfirmedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, dto.StaffName, dto.StartTime, salonName, mapsUrl, bookingUrl);
                _logger.LogInformation("[SMS] Onay bildirimi gönderildi: {Recipient}", recipient);
            }
            else
            {
                await _whatsAppService.SendAppointmentConfirmedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, dto.StaffName, dto.StartTime, salonName, mapsUrl, appointmentsUrl);
                _logger.LogInformation("[WHATSAPP] Onay bildirimi gönderildi: {Recipient}", recipient);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BİLDİRİM HATA] Onay bildirimi gönderilemedi: {Recipient}", recipient);
        }
    }

    public async Task SendAppointmentCancelledAsync(string recipient, AppointmentStatusDto dto)
    {
        try
        {
            var (channel, salonName, _, bookingUrl, _) = await GetTenantInfoAsync(dto.TenantId);

            if (channel == NotificationChannel.Sms)
            {
                await _smsService.SendAppointmentCancelledAsync(
                    recipient, dto.CustomerName, dto.StartTime, salonName, bookingUrl);
                _logger.LogInformation("[SMS] İptal bildirimi gönderildi: {Recipient}", recipient);
            }
            else
            {
                await _whatsAppService.SendAppointmentCancelledAsync(
                    recipient, dto.CustomerName, dto.StartTime, salonName, bookingUrl);
                _logger.LogInformation("[WHATSAPP] İptal bildirimi gönderildi: {Recipient}", recipient);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BİLDİRİM HATA] İptal bildirimi gönderilemedi: {Recipient}", recipient);
        }
    }

    public async Task SendAppointmentCompletedAsync(string recipient, AppointmentStatusDto dto, string reviewUrl)
    {
        try
        {
            var (channel, salonName, _, _, _) = await GetTenantInfoAsync(dto.TenantId);

            if (channel == NotificationChannel.Sms)
            {
                await _smsService.SendAppointmentCompletedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, salonName, reviewUrl);
                _logger.LogInformation("[SMS] Tamamlama bildirimi gönderildi: {Recipient}", recipient);
            }
            else
            {
                await _whatsAppService.SendAppointmentCompletedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, salonName, reviewUrl);
                _logger.LogInformation("[WHATSAPP] Tamamlama bildirimi gönderildi: {Recipient}", recipient);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BİLDİRİM HATA] Tamamlama bildirimi gönderilemedi: {Recipient}", recipient);
        }
    }

    public async Task SendAppointmentUpdatedAsync(string recipient, AppointmentStatusDto dto)
    {
        try
        {
            var (channel, salonName, _, bookingUrl, _) = await GetTenantInfoAsync(dto.TenantId);

            if (channel == NotificationChannel.Sms)
            {
                await _smsService.SendAppointmentUpdatedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, dto.StaffName, dto.StartTime, salonName, bookingUrl);
                _logger.LogInformation("[SMS] Güncelleme bildirimi gönderildi: {Recipient}", recipient);
            }
            else
            {
                await _whatsAppService.SendAppointmentUpdatedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, dto.StaffName, dto.StartTime, salonName, bookingUrl);
                _logger.LogInformation("[WHATSAPP] Güncelleme bildirimi gönderildi: {Recipient}", recipient);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BİLDİRİM HATA] Güncelleme bildirimi gönderilemedi: {Recipient}", recipient);
        }
    }

    private async Task<(NotificationChannel channel, string salonName, string mapsUrl, string bookingUrl, string subdomain)> GetTenantInfoAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        var subdomain  = tenant?.Subdomain ?? string.Empty;
        var mapsUrl    = !string.IsNullOrWhiteSpace(subdomain)
            ? $"{_frontendBase}/api/map/{subdomain}"
            : string.Empty;
        var bookingUrl = !string.IsNullOrWhiteSpace(subdomain)
            ? $"{_frontendBase}/book/{subdomain}"
            : string.Empty;

        return (
            tenant?.PreferredNotificationChannel ?? NotificationChannel.WhatsApp,
            tenant?.Name ?? string.Empty,
            mapsUrl,
            bookingUrl,
            subdomain
        );
    }
}
