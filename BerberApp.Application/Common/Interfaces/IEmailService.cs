namespace BerberApp.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationUrl);
    Task SendWelcomeEmailAsync(string toEmail, string fullName, string salonName);
}
