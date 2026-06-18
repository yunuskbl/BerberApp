using BerberApp.Application.Common.Interfaces;

namespace BerberApp.Infrastructure.Services;

/// <summary>
/// WhatsApp devre dışıyken kullanılan no-op implementasyon.
/// Hiçbir mesaj göndermez, hata fırlatmaz.
/// </summary>
public class NullWhatsAppService : IWhatsAppService
{
    public Task SendAppointmentConfirmedAsync(string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => Task.CompletedTask;

    public Task SendNewAppointmentRequestAsync(string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTime, int sequenceNumber)
        => Task.CompletedTask;

    public Task SendOtpAsync(string phone, string otp)
    {
        Console.WriteLine($"[OTP] {phone} → {otp}");
        return Task.CompletedTask;
    }

    public Task SendAppointmentCompletedAsync(string phone, string customerName, string serviceName,
        string salonName, string reviewUrl)
        => Task.CompletedTask;

    public Task SendAppointmentReminderAsync(string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "", string staffName = "")
        => Task.CompletedTask;

    public Task SendAppointmentReminder1hAsync(string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "", string staffName = "")
        => Task.CompletedTask;

    public Task SendAppointmentCancelledAsync(string phone, string customerName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
        => Task.CompletedTask;

    public Task SendAppointmentUpdatedAsync(string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => Task.CompletedTask;

    public Task SendMonthlyLimitWarningAsync(string phone, string salonName, int currentCount, int limit, bool isFull)
        => Task.CompletedTask;

    public Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
        => Task.CompletedTask;

    public Task SendCustomMessageAsync(string phone, string message, string? imageUrl = null)
        => Task.CompletedTask;
}
