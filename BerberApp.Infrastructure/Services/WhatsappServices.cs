using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace BerberApp.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly TwilioSettings _settings;
    private readonly System.Globalization.CultureInfo _tr = new("tr-TR");

    public WhatsAppService(IOptions<TwilioSettings> options)
    {
        _settings = options.Value;
        TwilioClient.Init(_settings.AccountSid, _settings.AuthToken);
    }

    // ── Content Template API (5 şablon) ──────────────────────────────────────

    public async Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var t = ToTurkeyTime(startTime);
        await SendTemplateAsync(phone, "RandevuOnay", new()
        {
            { "1", customerName },
            { "2", t.ToString("dd MMMM yyyy", _tr) },
            { "3", t.ToString("HH:mm") },
            { "4", serviceName },
            { "5", staffName },
            { "6", salonName },
            { "7", mapsUrl },
            { "8", bookingUrl },
        });
    }

    public async Task SendNewAppointmentRequestAsync(
        string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTime, int sequenceNumber)
    {
        var t = ToTurkeyTime(startTime);
        await SendTemplateAsync(staffPhone, "YeniRandevuBildirimi", new()
        {
            { "1", sequenceNumber.ToString() },
            { "2", customerName },
            { "3", customerPhone },
            { "4", serviceName },
            { "5", t.ToString("dd MMMM yyyy", _tr) },
            { "6", t.ToString("HH:mm") },
        });
    }

    public async Task SendOtpAsync(string phone, string otp)
    {
        await SendTemplateAsync(phone, "OtpDogrulama", new()
        {
            { "1", otp },
        });
    }

    public async Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName,
        string salonName, string reviewUrl)
    {
        await SendTemplateAsync(phone, "HizmetTamamlandi", new()
        {
            { "1", customerName },
            { "2", serviceName },
            { "3", salonName },
            { "4", reviewUrl },
        });
    }

    public async Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var t = ToTurkeyTime(startTime);
        await SendTemplateAsync(phone, "RandevuHatirlatma", new()
        {
            { "1", customerName },
            { "2", "24 saat" },
            { "3", t.ToString("dd MMMM yyyy", _tr) },
            { "4", t.ToString("HH:mm") },
            { "5", serviceName },
            { "6", staffName },
            { "7", salonName },
            { "8", mapsUrl },
        });
    }

    public async Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var t = ToTurkeyTime(startTime);
        await SendTemplateAsync(phone, "RandevuHatirlatma", new()
        {
            { "1", customerName },
            { "2", "1 saat" },
            { "3", t.ToString("dd MMMM yyyy", _tr) },
            { "4", t.ToString("HH:mm") },
            { "5", serviceName },
            { "6", staffName },
            { "7", salonName },
            { "8", mapsUrl },
        });
    }

    // ── Serbest metin mesajları (şablon dışı) ─────────────────────────────────

    public async Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? " Yeni randevu için salonumuzu arayabilirsiniz." : $"\n\n🔗 *Yeni randevu almak için:*\n{bookingUrl}";

        await SendTextAsync(phone, $"""
            ✂ *ayarlıyo - Randevu İptali*

            Merhaba {customerName},{salonLine}

            {t.ToString("dd MMMM yyyy", _tr)} tarihli {t:HH:mm} saatindeki randevunuz iptal edilmiştir.{bookingLine}
            """);
    }

    public async Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $"\n\n🔗 *Yeni randevu almak için:*\n{bookingUrl}";

        await SendTextAsync(phone, $"""
            ✂ *ayarlıyo - Randevu Güncellendi*

            Merhaba {customerName}! 👋

            Randevunuz güncellendi. Yeni bilgiler:

            📅 Tarih: {t.ToString("dd MMMM yyyy", _tr)}
            ⏰ Saat: {t:HH:mm}
            💈 Hizmet: {serviceName}
            👤 Personel: {staffName}{salonLine}{bookingLine}

            Sorularınız için salonumuzu arayabilirsiniz.
            """);
    }

    public async Task SendMonthlyLimitWarningAsync(
        string phone, string salonName, int currentCount, int limit, bool isFull)
    {
        var msg = isFull
            ? $"""
               ⚠️ *ayarlıyo - Aylık Randevu Limitiniz Doldu!*

               Merhaba {salonName}!

               Bu ay {limit} randevu limitinize ulaştınız. 🚫

               Yeni randevular bu ay alınamayacak. Limitinizi artırmak için planınızı yükseltin.

               👉 Paketinizi yükseltmek için: app.ayarliyo.com/pricing
               """
            : $"""
               ⚠️ *ayarlıyo - Aylık Randevu Limitine Yaklaşıyorsunuz!*

               Merhaba {salonName}!

               Bu ay {currentCount}/{limit} randevu kullandınız. (%80)

               Limitinize yaklaşıyorsunuz. Kesintisiz hizmet için planınızı yükseltmeyi düşünün.

               👉 Paketleri incelemek için: app.ayarliyo.com/pricing
               """;

        await SendTextAsync(phone, msg);
    }

    public async Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
    {
        var msg = daysLeft <= 1
            ? $"""
               🚨 *ayarlıyo - Aboneliğiniz Yarın Sona Eriyor!*

               Merhaba {salonName}!

               Aboneliğiniz yarın sona erecek. 😟

               Kesintisiz hizmet için aboneliğinizi hemen yenileyin.

               👉 Yenilemek için: app.ayarliyo.com/payment
               """
            : $"""
               ⏰ *ayarlıyo - Aboneliğiniz {daysLeft} Gün İçinde Sona Eriyor*

               Merhaba {salonName}!

               Aboneliğinizin sona ermesine {daysLeft} gün kaldı.

               Hizmet kesintisi yaşamamak için aboneliğinizi önceden yenileyin.

               👉 Yenilemek için: app.ayarliyo.com/payment
               """;

        await SendTextAsync(phone, msg);
    }

    public async Task SendCustomMessageAsync(string phone, string message)
        => await SendTextAsync(phone, message);

    // ── Özel gönderim yardımcıları ────────────────────────────────────────────

    /// <summary>Twilio Content Template API ile şablonlu mesaj gönderir.</summary>
    private async Task SendTemplateAsync(
        string phone, string templateKey, Dictionary<string, string> variables)
    {
        if (!_settings.Templates.TryGetValue(templateKey, out var sid))
            throw new InvalidOperationException($"Şablon SID bulunamadı: {templateKey}");

        var contentVariables = JsonSerializer.Serialize(variables);

        await MessageResource.CreateAsync(
            from: new PhoneNumber(_settings.WhatsAppFrom),
            to:   new PhoneNumber($"whatsapp:{FormatPhone(phone)}"),
            contentSid:       sid,
            contentVariables: contentVariables
        );
    }

    /// <summary>Şablon SID gerektirmeyen serbest metin mesajı gönderir.</summary>
    private async Task SendTextAsync(string phone, string body)
    {
        // Serbest metin için eski (sandbox veya onaylı) from numarasını kullan
        var from = !string.IsNullOrWhiteSpace(_settings.WhatsAppFrom)
            ? _settings.WhatsAppFrom
            : _settings.FromNumber;

        await MessageResource.CreateAsync(
            body: body,
            from: new PhoneNumber(from),
            to:   new PhoneNumber($"whatsapp:{FormatPhone(phone)}")
        );
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────

    private static string FormatPhone(string phone)
    {
        // E.164: 05551234567 → +905551234567
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (phone.StartsWith("0"))       return "+90" + phone[1..];
        if (!phone.StartsWith("+"))      return "+90" + phone;
        return phone;
    }

    private static DateTime ToTurkeyTime(DateTime utcTime)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        try
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime,
                TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"));
        }
        catch
        {
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime,
                TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"));
        }
    }
}
