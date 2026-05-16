using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace BerberApp.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly string _smtpHost;
    private readonly int _smtpPort;
    private readonly string _smtpUsername;
    private readonly string _smtpPassword;
    private readonly string _fromAddress;
    private readonly string _fromName;

    public SmtpEmailService(IConfiguration config)
    {
        _smtpHost     = config["Email:SmtpHost"]     ?? "smtp.gmail.com";
        _smtpPort     = int.Parse(config["Email:SmtpPort"] ?? "587");
        _smtpUsername = config["Email:SmtpUsername"] ?? "";
        _smtpPassword = config["Email:SmtpPassword"] ?? "";
        _fromAddress  = config["Email:FromAddress"]  ?? "noreply@ayarliyo.com";
        _fromName     = config["Email:FromName"]     ?? "ayarlıyo";
    }

    public async Task SendEmailVerificationAsync(string toEmail, string fullName, string verificationUrl)
    {
        var body = $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;letter-spacing:-1px;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:#111827;padding-bottom:12px;">
                      Merhaba {fullName}! 👋
                    </td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:28px;">
                      Kayıt olduğunuz için teşekkürler. İşletme hesabınızı etkinleştirmek için aşağıdaki butona tıklayın. Bu bağlantı <strong>24 saat</strong> geçerlidir.
                    </td></tr>
                    <tr><td align="center" style="padding-bottom:28px;">
                      <a href="{verificationUrl}" style="display:inline-block;padding:14px 36px;background:#111827;color:#fff;border-radius:10px;font-size:15px;font-weight:700;text-decoration:none;letter-spacing:-0.3px;">
                        E-posta Adresimi Doğrula
                      </a>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Bu e-postayı siz talep etmediyseniz güvenle görmezden gelebilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        await SendAsync(toEmail, "E-posta Adresinizi Doğrulayın — ayarlıyo", body);
    }

    public async Task SendWelcomeEmailAsync(string toEmail, string fullName, string salonName)
    {
        var body = $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;letter-spacing:-1px;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:#111827;padding-bottom:12px;">
                      Hoş geldiniz, {fullName}! 🎉
                    </td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:24px;">
                      <strong>{salonName}</strong> işletmenizin hesabı başarıyla oluşturuldu. 14 günlük ücretsiz deneme süreniz başladı.
                    </td></tr>
                    <tr><td align="center" style="padding-bottom:28px;">
                      <a href="https://ayarliyo.com/dashboard" style="display:inline-block;padding:14px 36px;background:#111827;color:#fff;border-radius:10px;font-size:15px;font-weight:700;text-decoration:none;">
                        Panele Git
                      </a>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Sorularınız için destek@ayarliyo.com adresine yazabilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        await SendAsync(toEmail, $"Hoş Geldiniz, {salonName}! — ayarlıyo", body);
    }

    private async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        using var message = new MailMessage
        {
            From = new MailAddress(_fromAddress, _fromName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(to);

        await client.SendMailAsync(message);
    }
}
