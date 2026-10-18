using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Interfaces;
using Hangfire;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Jobs;

/// <summary>
/// Randevu bildirimlerini arka planda gönderen Hangfire işi.
///
/// Gerçek gönderim mantığı <see cref="LinkNotificationService"/>'te kalıyor;
/// bu sınıf yalnızca onu istek dışında, yeniden denenebilir bir bağlamda
/// çalıştırıyor.
/// </summary>
public class NotificationJob
{
    private readonly LinkNotificationService _inner;
    private readonly ILogger<NotificationJob> _log;

    public NotificationJob(LinkNotificationService inner, ILogger<NotificationJob> log)
    {
        _inner = inner;
        _log   = log;
    }

    // WhatsApp/SMS tarafındaki geçici arızalarda birkaç deneme mantıklı; ama
    // sınırsız değil, yoksa aynı bildirim müşteriye defalarca gidebilir.
    [Queue("whatsapp")]
    [AutomaticRetry(Attempts = 2)]
    public Task Received(string recipient, AppointmentStatusDto dto)
        => _inner.SendAppointmentReceivedAsync(recipient, dto);

    [Queue("whatsapp")]
    [AutomaticRetry(Attempts = 2)]
    public Task Confirmed(string recipient, AppointmentStatusDto dto)
        => _inner.SendAppointmentConfirmedAsync(recipient, dto);

    [Queue("whatsapp")]
    [AutomaticRetry(Attempts = 2)]
    public Task Cancelled(string recipient, AppointmentStatusDto dto)
        => _inner.SendAppointmentCancelledAsync(recipient, dto);

    [Queue("whatsapp")]
    [AutomaticRetry(Attempts = 2)]
    public Task Completed(string recipient, AppointmentStatusDto dto, string reviewUrl)
        => _inner.SendAppointmentCompletedAsync(recipient, dto, reviewUrl);

    [Queue("whatsapp")]
    [AutomaticRetry(Attempts = 2)]
    public Task Updated(string recipient, AppointmentStatusDto dto)
        => _inner.SendAppointmentUpdatedAsync(recipient, dto);

    /// <summary>Randevu sayfasından gelen talebin işletmeye/personele bildirimi.</summary>
    [Queue("whatsapp")]
    [AutomaticRetry(Attempts = 2)]
    public async Task NewAppointmentRequest(
        Guid tenantId, string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTimeTurkey, int sequenceNumber)
    {
        try
        {
            await _inner.SendNewAppointmentRequestAsync(
                tenantId, staffPhone, customerName, customerPhone,
                serviceName, startTimeTurkey, sequenceNumber);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[BİLDİRİM] Yeni randevu bildirimi gönderilemedi: {Phone}", staffPhone);
            throw;
        }
    }
}
