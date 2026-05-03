using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Services;

public class LinkNotificationService : INotificationService
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly ISmsService _smsService;
    private readonly ILogger<LinkNotificationService> _logger;

    public LinkNotificationService(
        IAppDbContext context,
        IWhatsAppService whatsAppService,
        ISmsService smsService,
        ILogger<LinkNotificationService> logger)
    {
        _context = context;
        _whatsAppService = whatsAppService;
        _smsService = smsService;
        _logger = logger;
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
            var (channel, salonName) = await GetTenantInfoAsync(dto.TenantId);

            if (channel == NotificationChannel.Sms)
            {
                await _smsService.SendAppointmentConfirmedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, dto.StaffName, dto.StartTime, salonName);
                _logger.LogInformation("[SMS] Onay bildirimi gönderildi: {Recipient}", recipient);
            }
            else
            {
                await _whatsAppService.SendAppointmentConfirmedAsync(
                    recipient, dto.CustomerName, dto.ServiceName, dto.StaffName, dto.StartTime, salonName);
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
            var (channel, salonName) = await GetTenantInfoAsync(dto.TenantId);

            if (channel == NotificationChannel.Sms)
            {
                await _smsService.SendAppointmentCancelledAsync(
                    recipient, dto.CustomerName, dto.StartTime, salonName);
                _logger.LogInformation("[SMS] İptal bildirimi gönderildi: {Recipient}", recipient);
            }
            else
            {
                await _whatsAppService.SendAppointmentCancelledAsync(
                    recipient, dto.CustomerName, dto.StartTime, salonName);
                _logger.LogInformation("[WHATSAPP] İptal bildirimi gönderildi: {Recipient}", recipient);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[BİLDİRİM HATA] İptal bildirimi gönderilemedi: {Recipient}", recipient);
        }
    }

    private async Task<(NotificationChannel channel, string salonName)> GetTenantInfoAsync(Guid tenantId)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == tenantId);

        return (
            tenant?.PreferredNotificationChannel ?? NotificationChannel.WhatsApp,
            tenant?.Name ?? string.Empty
        );
    }
}
