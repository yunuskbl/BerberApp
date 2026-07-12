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
    private readonly string _adminNotificationAddress;

    public SmtpEmailService(IConfiguration config)
    {
        _smtpHost     = config["Email:SmtpHost"]     ?? "smtp.gmail.com";
        _smtpPort     = int.Parse(config["Email:SmtpPort"] ?? "587");
        _smtpUsername = config["Email:SmtpUsername"] ?? "";
        _smtpPassword = config["Email:SmtpPassword"] ?? "";
        _fromAddress  = string.IsNullOrEmpty(config["Email:FromAddress"]) ? "noreply@ayarliyo.com" : config["Email:FromAddress"]!;
        _fromName     = config["Email:FromName"]     ?? "ayarlıyo";
        // Yeni kayıt bildirimleri buraya gider; ayarlanmamışsa SMTP hesabına düşer
        _adminNotificationAddress = string.IsNullOrEmpty(config["Email:AdminNotificationAddress"])
            ? _smtpUsername
            : config["Email:AdminNotificationAddress"]!;
    }

    public string AdminNotificationAddress => _adminNotificationAddress;

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

    public async Task SendAppointmentConfirmedAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate, string serviceName, string staffName)
    {
        var dateStr = appointmentDate.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("tr-TR"));
        var body = $"""
            <!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:#111827;padding-bottom:16px;">Randevunuz Onaylandı ✅</td></tr>
                    <tr><td style="background:#f9fafb;border-radius:12px;padding:20px;margin-bottom:20px;">
                      <p style="margin:0 0 8px;font-size:14px;color:#6b7280;">Müşteri</p>
                      <p style="margin:0 0 16px;font-size:16px;font-weight:600;color:#111827;">{customerName}</p>
                      <p style="margin:0 0 8px;font-size:14px;color:#6b7280;">Salon</p>
                      <p style="margin:0 0 16px;font-size:16px;font-weight:600;color:#111827;">{salonName}</p>
                      <p style="margin:0 0 8px;font-size:14px;color:#6b7280;">Hizmet</p>
                      <p style="margin:0 0 16px;font-size:16px;font-weight:600;color:#111827;">{serviceName} — {staffName}</p>
                      <p style="margin:0 0 8px;font-size:14px;color:#6b7280;">Tarih & Saat</p>
                      <p style="margin:0;font-size:18px;font-weight:700;color:#111827;">{dateStr}</p>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Sorularınız için destek@ayarliyo.com adresine yazabilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
        await SendAsync(toEmail, $"Randevunuz Onaylandı — {salonName}", body);
    }

    public async Task SendAppointmentCancelledAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate)
    {
        var dateStr = appointmentDate.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("tr-TR"));
        var body = $"""
            <!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:#111827;padding-bottom:16px;">Randevunuz İptal Edildi ❌</td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:20px;">
                      Sayın <strong>{customerName}</strong>, <strong>{dateStr}</strong> tarihli randevunuz <strong>{salonName}</strong> tarafından iptal edildi.
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Yeni bir randevu almak için salona ait rezervasyon sayfasını ziyaret edebilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
        await SendAsync(toEmail, $"Randevunuz İptal Edildi — {salonName}", body);
    }

    public async Task SendAppointmentReminderAsync(string toEmail, string customerName, string salonName, DateTime appointmentDate, string serviceName)
    {
        var dateStr = appointmentDate.ToString("dd MMMM yyyy, HH:mm", new System.Globalization.CultureInfo("tr-TR"));
        var body = $"""
            <!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:#111827;padding-bottom:16px;">Randevu Hatırlatması 🔔</td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:20px;">
                      Sayın <strong>{customerName}</strong>, yarın <strong>{salonName}</strong>'daki <strong>{serviceName}</strong> randevunuzu hatırlatmak istedik.
                    </td></tr>
                    <tr><td style="background:#f9fafb;border-radius:12px;padding:16px;text-align:center;margin-bottom:20px;">
                      <p style="margin:0;font-size:20px;font-weight:700;color:#111827;">{dateStr}</p>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      İptal veya değişiklik için lütfen salonla iletişime geçin.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
        await SendAsync(toEmail, $"Yarın Randevunuz Var — {salonName}", body);
    }

    public async Task SendSubscriptionActivatedAsync(string toEmail, string fullName, string planName, DateTime expiryDate)
    {
        var dateStr = expiryDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        var body = $"""
            <!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:#111827;padding-bottom:16px;">Aboneliğiniz Aktifleşti 🎉</td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:20px;">
                      Merhaba <strong>{fullName}</strong>, <strong>{planName}</strong> planınız başarıyla aktifleştirildi. Bir sonraki yenileme tarihi: <strong>{dateStr}</strong>.
                    </td></tr>
                    <tr><td align="center" style="padding-bottom:28px;">
                      <a href="https://ayarliyo.com/dashboard" style="display:inline-block;padding:14px 36px;background:#111827;color:#fff;border-radius:10px;font-size:15px;font-weight:700;text-decoration:none;">Panele Git</a>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Fatura ve abonelik detayları için destek@ayarliyo.com adresine yazabilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
        await SendAsync(toEmail, $"Aboneliğiniz Aktifleşti — ayarlıyo {planName}", body);
    }

    public async Task SendSubscriptionExpiryWarningAsync(string toEmail, string fullName, string salonName, int daysRemaining, DateTime expiryDate)
    {
        var dateStr = expiryDate.ToString("dd MMMM yyyy", new System.Globalization.CultureInfo("tr-TR"));
        var body = $"""
            <!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:#dc2626;padding-bottom:16px;">Aboneliğiniz Sona Eriyor ⚠️</td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:20px;">
                      Merhaba <strong>{fullName}</strong>, <strong>{salonName}</strong> hesabınızın aboneliği <strong>{daysRemaining} gün içinde</strong> ({dateStr}) sona erecek.
                      Hizmet kesintisi yaşamamak için aboneliğinizi yenileyin.
                    </td></tr>
                    <tr><td align="center" style="padding-bottom:28px;">
                      <a href="https://ayarliyo.com/pricing" style="display:inline-block;padding:14px 36px;background:#dc2626;color:#fff;border-radius:10px;font-size:15px;font-weight:700;text-decoration:none;">Aboneliği Yenile</a>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Sorularınız için destek@ayarliyo.com adresine yazabilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
        await SendAsync(toEmail, $"Aboneliğiniz {daysRemaining} Gün İçinde Sona Eriyor — ayarlıyo", body);
    }

    public async Task SendMonthlyLimitWarningAsync(string toEmail, string salonName, int currentCount, int limit, bool isFull)
    {
        var title = isFull ? "Aylık Randevu Limitiniz Doldu 🚫" : "Aylık Randevu Limitine Yaklaşıyorsunuz ⚠️";
        var msg = isFull
            ? $"<strong>{salonName}</strong> için bu ayki <strong>{limit}</strong> randevu limitiniz doldu. Yeni randevular bu ay alınamayacak. Limitinizi artırmak için planınızı yükseltin."
            : $"<strong>{salonName}</strong> için bu ay <strong>{currentCount}/{limit}</strong> randevu kullandınız (%80). Kesintisiz hizmet için planınızı yükseltmeyi düşünün.";
        var color = isFull ? "#dc2626" : "#d97706";
        var body = $"""
            <!DOCTYPE html><html lang="tr"><head><meta charset="UTF-8"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:48px 40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td align="center" style="padding-bottom:28px;">
                      <span style="font-size:28px;font-weight:800;color:#111827;">ayarlıyo<span style="display:inline-block;width:8px;height:8px;background:#111827;border-radius:50%;margin-left:2px;vertical-align:middle;"></span></span>
                    </td></tr>
                    <tr><td style="font-size:22px;font-weight:700;color:{color};padding-bottom:16px;">{title}</td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:24px;">{msg}</td></tr>
                    <tr><td align="center" style="padding-bottom:28px;">
                      <a href="https://ayarliyo.com/pricing" style="display:inline-block;padding:14px 36px;background:{color};color:#fff;border-radius:10px;font-size:15px;font-weight:700;text-decoration:none;">Planı Yükselt</a>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Sorularınız için destek@ayarliyo.com adresine yazabilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body></html>
            """;
        await SendAsync(toEmail, isFull ? "Aylık Randevu Limitiniz Doldu — ayarlıyo" : "Aylık Randevu Limitine Yaklaşıyorsunuz — ayarlıyo", body);
    }

    public async Task SendOtpAsync(string toEmail, string otp)
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
                    <tr><td style="font-size:20px;font-weight:700;color:#111827;padding-bottom:12px;">
                      Doğrulama Kodunuz 🔐
                    </td></tr>
                    <tr><td style="font-size:15px;color:#6b7280;line-height:1.6;padding-bottom:28px;">
                      Kayıt işleminizi tamamlamak için aşağıdaki kodu kullanın. Bu kod <strong>5 dakika</strong> geçerlidir.
                    </td></tr>
                    <tr><td align="center" style="padding-bottom:28px;">
                      <div style="display:inline-block;padding:20px 40px;background:#f9fafb;border:2px solid #e5e7eb;border-radius:12px;">
                        <span style="font-size:36px;font-weight:800;color:#111827;letter-spacing:8px;">{otp}</span>
                      </div>
                    </td></tr>
                    <tr><td style="font-size:13px;color:#9ca3af;border-top:1px solid #f3f4f6;padding-top:20px;">
                      Bu kodu kimseyle paylaşmayın. Eğer bu işlemi siz yapmadıysanız görmezden gelebilirsiniz.
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        await SendAsync(toEmail, "Doğrulama Kodunuz — ayarlıyo", body);
    }

    public async Task SendNewTenantRegisteredAsync(string tenantName, string subdomain, string ownerName, string ownerEmail, string phone)
    {
        if (string.IsNullOrWhiteSpace(_adminNotificationAddress))
            return;

        var when = DateTime.UtcNow.AddHours(3).ToString("dd.MM.yyyy HH:mm"); // TR saati
        var body = $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width, initial-scale=1.0"></head>
            <body style="margin:0;padding:0;background:#f3f4f6;font-family:'Inter',Arial,sans-serif;">
              <table width="100%" cellpadding="0" cellspacing="0" style="padding:40px 0;">
                <tr><td align="center">
                  <table width="520" cellpadding="0" cellspacing="0" style="background:#fff;border-radius:16px;padding:40px;box-shadow:0 4px 24px rgba(0,0,0,0.08);">
                    <tr><td style="font-size:20px;font-weight:700;color:#111827;padding-bottom:16px;">
                      🏪 Yeni İşletme Kaydı
                    </td></tr>
                    <tr><td style="font-size:15px;color:#374151;line-height:1.9;">
                      <strong>İşletme:</strong> {tenantName}<br/>
                      <strong>Subdomain:</strong> {subdomain}<br/>
                      <strong>Yetkili:</strong> {ownerName}<br/>
                      <strong>E-posta:</strong> {ownerEmail}<br/>
                      <strong>Telefon:</strong> {phone}<br/>
                      <strong>Tarih:</strong> {when}
                    </td></tr>
                  </table>
                </td></tr>
              </table>
            </body>
            </html>
            """;

        await SendAsync(_adminNotificationAddress, $"Yeni işletme kaydı: {tenantName}", body);
    }

    protected virtual async Task SendAsync(string to, string subject, string htmlBody)
    {
        using var client = new SmtpClient(_smtpHost, _smtpPort)
        {
            Credentials = new NetworkCredential(_smtpUsername, _smtpPassword),
            EnableSsl = true,
            DeliveryMethod = SmtpDeliveryMethod.Network
        };

        var plainText = System.Text.RegularExpressions.Regex.Replace(htmlBody, "<[^>]+>", "").Trim();

        using var message = new MailMessage
        {
            From       = new MailAddress(_fromAddress, _fromName),
            Subject    = subject,
            IsBodyHtml = false,
            Body       = plainText,
        };
        message.To.Add(to);
        message.Headers.Add("X-Mailer", "ayarliyo");
        message.Headers.Add("Precedence", "transactional");

        var htmlView  = AlternateView.CreateAlternateViewFromString(htmlBody,  null, "text/html");
        var plainView = AlternateView.CreateAlternateViewFromString(plainText, null, "text/plain");
        message.AlternateViews.Add(plainView);
        message.AlternateViews.Add(htmlView);

        await client.SendMailAsync(message);
    }
}
