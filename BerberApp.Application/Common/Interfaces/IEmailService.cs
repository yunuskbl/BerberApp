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
}
