using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Infrastructure.Jobs;
using Hangfire;

namespace BerberApp.Infrastructure.Services;

/// <summary>
/// Bildirimleri göndermez, kuyruğa alır.
///
/// Eskiden randevu oluşturma/onaylama/iptal isteklerinin hepsi bildirim
/// gönderimini bekliyordu. WhatsApp tarafı yavaşladığında istek dakikalarca
/// açık kalıp ters proxy tarafından düşürülüyor, randevu kaydedilmiş olmasına
/// rağmen kullanıcı hata görüyordu. Bildirimin başarısı isteğin sonucunu
/// değiştirmediği için (çağıranlar hatayı zaten yutuyordu) kuyruğa almak hem
/// isteği hızlandırıyor hem de başarısız gönderimlerin yeniden denenmesini
/// sağlıyor.
///
/// <see cref="INotificationService"/> arkasında durduğu için çağıran
/// handler'ların hiçbirinde değişiklik gerekmiyor.
/// </summary>
public class QueuedNotificationService : INotificationService
{
    private readonly IBackgroundJobClient _jobs;

    public QueuedNotificationService(IBackgroundJobClient jobs) => _jobs = jobs;

    public Task SendAppointmentReceivedAsync(string recipient, AppointmentStatusDto dto)
    {
        _jobs.Enqueue<NotificationJob>(j => j.Received(recipient, dto));
        return Task.CompletedTask;
    }

    public Task SendAppointmentConfirmedAsync(string recipient, AppointmentStatusDto dto)
    {
        _jobs.Enqueue<NotificationJob>(j => j.Confirmed(recipient, dto));
        return Task.CompletedTask;
    }

    public Task SendAppointmentCancelledAsync(string recipient, AppointmentStatusDto dto)
    {
        _jobs.Enqueue<NotificationJob>(j => j.Cancelled(recipient, dto));
        return Task.CompletedTask;
    }

    public Task SendAppointmentCompletedAsync(string recipient, AppointmentStatusDto dto, string reviewUrl)
    {
        _jobs.Enqueue<NotificationJob>(j => j.Completed(recipient, dto, reviewUrl));
        return Task.CompletedTask;
    }

    public Task SendAppointmentUpdatedAsync(string recipient, AppointmentStatusDto dto)
    {
        _jobs.Enqueue<NotificationJob>(j => j.Updated(recipient, dto));
        return Task.CompletedTask;
    }

    public Task SendNewAppointmentRequestAsync(
        Guid tenantId, string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTimeTurkey, int sequenceNumber)
    {
        _jobs.Enqueue<NotificationJob>(j => j.NewAppointmentRequest(
            tenantId, staffPhone, customerName, customerPhone,
            serviceName, startTimeTurkey, sequenceNumber));
        return Task.CompletedTask;
    }
}
