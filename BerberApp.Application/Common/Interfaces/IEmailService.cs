namespace BerberApp.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationUrl);
    Task SendWelcomeEmailAsync(string toEmail, string fullName, string salonName);
    Task SendAppointmentConfirmedAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate, string serviceName, string staffName);
    Task SendAppointmentCancelledAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate);
    Task SendAppointmentReminderAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate, string serviceName);
    Task SendSubscriptionActivatedAsync(string toEmail, string fullName, string planName, DateTime expiryDate);
    /// <summary>
    /// Abonelik/deneme bitiş uyarısı. Kullanım rakamları işletmenin dönem
    /// boyunca sistemden ne aldığını gösteriyor; soyut bir yükseltme çağrısı
    /// yerine kendi verisi konuşuyor.
    /// </summary>
    Task SendSubscriptionExpiryWarningAsync(
        string toEmail, string fullName, string salonName,
        int daysRemaining, DateTime expiryDate,
        bool isTrial = false,
        int appointments = 0, int customers = 0, int notifications = 0,
        decimal revenueInTry = 0m);
    Task SendMonthlyLimitWarningAsync(string toEmail, string salonName, int currentCount, int limit, bool isFull);
    /// <param name="salonName">Kodun hangi işletme için istendiği. Salon bağlamı olmayan akışlarda boş.</param>
    Task SendOtpAsync(string toEmail, string otp, string salonName = "");

    /// <summary>Yeni işletme kaydı olduğunda platform yöneticisine bilgi e-postası (adres implementasyonda yapılandırılır).</summary>
    Task SendNewTenantRegisteredAsync(string tenantName, string subdomain, string ownerName, string ownerEmail, string phone);
}
