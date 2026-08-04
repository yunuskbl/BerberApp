namespace BerberApp.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationUrl);
    Task SendWelcomeEmailAsync(string toEmail, string fullName, string salonName);
    Task SendAppointmentConfirmedAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate, string serviceName, string staffName);
    Task SendAppointmentCancelledAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate);
    Task SendAppointmentReminderAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate, string serviceName);
    Task SendSubscriptionActivatedAsync(string toEmail, string fullName, string planName, DateTime expiryDate);
    Task SendSubscriptionExpiryWarningAsync(string toEmail, string fullName, string salonName, int daysRemaining, DateTime expiryDate);
    Task SendMonthlyLimitWarningAsync(string toEmail, string salonName, int currentCount, int limit, bool isFull);
    /// <param name="salonName">Kodun hangi işletme için istendiği. Salon bağlamı olmayan akışlarda boş.</param>
    Task SendOtpAsync(string toEmail, string otp, string salonName = "");

    /// <summary>Yeni işletme kaydı olduğunda platform yöneticisine bilgi e-postası (adres implementasyonda yapılandırılır).</summary>
    Task SendNewTenantRegisteredAsync(string tenantName, string subdomain, string ownerName, string ownerEmail, string phone);
}
