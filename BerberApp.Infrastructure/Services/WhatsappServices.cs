using System.Net.Http.Headers;
using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using Microsoft.Extensions.Options;

namespace BerberApp.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly MetaWhatsAppSettings _settings;
    private readonly System.Globalization.CultureInfo _tr = new("tr-TR");

    public WhatsAppService(HttpClient http, IOptions<MetaWhatsAppSettings> options)
    {
        _settings = options.Value;
        _http = http;
        _http.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", _settings.AccessToken);
    }

    // ── Bildirim mesajları ────────────────────────────────────────────────────

    public async Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var t         = ToTurkeyTime(startTime);
        var salonLine = string.IsNullOrWhiteSpace(salonName) ? "" : $"\n🏪 Salon: {salonName}";
        var mapsLine  = string.IsNullOrWhiteSpace(mapsUrl)   ? "" : $"\n📍 Konum: {mapsUrl}";
        var bookLine  = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $"\n📋 Randevularım: {bookingUrl}";

        await SendTextAsync(phone, $"""
            ✅ ayarlıyo - Randevunuz Onaylandı!

            Merhaba {customerName}!

            Randevunuz başarıyla oluşturuldu.

            📅 Tarih: {t.ToString("dd MMMM yyyy", _tr)}
            ⏰ Saat: {t:HH:mm}
            🔹 Hizmet: {serviceName}
            👤 Personel: {staffName}{salonLine}{mapsLine}{bookLine}

            Görüşmek üzere! 👋
            """);
    }

    public async Task SendNewAppointmentRequestAsync(
        string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTime, int sequenceNumber)
    {
        var t = ToTurkeyTime(startTime);

        await SendTextAsync(staffPhone, $"""
            🔔 ayarlıyo - Yeni Randevu Talebi #{sequenceNumber}

            👤 Müşteri: {customerName}
            📞 Telefon: {customerPhone}
            🔹 Hizmet: {serviceName}
            📅 Tarih: {t.ToString("dd MMMM yyyy", _tr)}
            ⏰ Saat: {t:HH:mm}

            Onaylamak için ONAYLA {sequenceNumber}
            Reddetmek için REDDET {sequenceNumber}
            """);
    }

    public async Task SendOtpAsync(string phone, string otp)
    {
        await SendTemplateAsync(phone, "OtpDogrulama", otp);
    }

    public async Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName,
        string salonName, string reviewUrl)
    {
        var reviewLine = string.IsNullOrWhiteSpace(reviewUrl) ? "" : $"\n\n⭐ Değerlendirmenizi bekliyoruz:\n{reviewUrl}";

        await SendTextAsync(phone, $"""
            🎉 ayarlıyo - Hizmetiniz Tamamlandı!

            Merhaba {customerName}!

            {salonName} olarak sizi aramızda görmekten mutluluk duyduk.
            🔹 Hizmet: {serviceName}{reviewLine}

            Tekrar görüşmek üzere! 👋
            """);
    }

    public async Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var t         = ToTurkeyTime(startTime);
        var salonLine = string.IsNullOrWhiteSpace(salonName) ? "" : $"\n🏪 Salon: {salonName}";
        var staffLine = string.IsNullOrWhiteSpace(staffName) ? "" : $"\n👤 Personel: {staffName}";
        var mapsLine  = string.IsNullOrWhiteSpace(mapsUrl)   ? "" : $"\n📍 Konum: {mapsUrl}";

        await SendTextAsync(phone, $"""
            ⏰ ayarlıyo - Yarınki Randevunuzu Hatırlatalım!

            Merhaba {customerName}!

            Randevunuz 24 saat sonra!

            📅 Tarih: {t.ToString("dd MMMM yyyy", _tr)}
            ⏰ Saat: {t:HH:mm}
            🔹 Hizmet: {serviceName}{staffLine}{salonLine}{mapsLine}

            Görüşmek üzere! 👋
            """);
    }

    public async Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var t         = ToTurkeyTime(startTime);
        var salonLine = string.IsNullOrWhiteSpace(salonName) ? "" : $"\n🏪 Salon: {salonName}";
        var staffLine = string.IsNullOrWhiteSpace(staffName) ? "" : $"\n👤 Personel: {staffName}";
        var mapsLine  = string.IsNullOrWhiteSpace(mapsUrl)   ? "" : $"\n📍 Konum: {mapsUrl}";

        await SendTextAsync(phone, $"""
            🔔 ayarlıyo - Randevunuz 1 Saat Sonra!

            Merhaba {customerName}!

            📅 Tarih: {t.ToString("dd MMMM yyyy", _tr)}
            ⏰ Saat: {t:HH:mm}
            🔹 Hizmet: {serviceName}{staffLine}{salonLine}{mapsLine}

            Sizi bekliyoruz! 👋
            """);
    }

    // ── Serbest metin mesajları ───────────────────────────────────────────────

    public async Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl)
            ? " Yeni randevu için salonumuzu arayabilirsiniz."
            : $"\n\n🔗 Yeni randevu almak için:\n{bookingUrl}";

        await SendTextAsync(phone, $"""
            ❌ ayarlıyo - Randevu İptali

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
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $"\n\n🔗 Yeni randevu almak için:\n{bookingUrl}";

        await SendTextAsync(phone, $"""
            🔄 ayarlıyo - Randevu Güncellendi

            Merhaba {customerName}!

            Randevunuz güncellendi. Yeni bilgiler:

            📅 Tarih: {t.ToString("dd MMMM yyyy", _tr)}
            ⏰ Saat: {t:HH:mm}
            🔹 Hizmet: {serviceName}
            👤 Personel: {staffName}{salonLine}{bookingLine}

            Sorularınız için bizi arayabilirsiniz.
            """);
    }

    public async Task SendMonthlyLimitWarningAsync(
        string phone, string salonName, int currentCount, int limit, bool isFull)
    {
        var msg = isFull
            ? $"⚠️ ayarlıyo - Aylık Randevu Limitiniz Doldu!\n\nMerhaba {salonName}!\n\nBu ay {limit} randevu limitinize ulaştınız. 🚫\n\nYeni randevular bu ay alınamayacak. Limitinizi artırmak için planınızı yükseltin.\n\n👉 app.ayarliyo.com/pricing"
            : $"⚠️ ayarlıyo - Aylık Randevu Limitine Yaklaşıyorsunuz!\n\nMerhaba {salonName}!\n\nBu ay {currentCount}/{limit} randevu kullandınız. (%80)\n\nLimitinize yaklaşıyorsunuz. Kesintisiz hizmet için planınızı yükseltmeyi düşünün.\n\n👉 app.ayarliyo.com/pricing";

        await SendTextAsync(phone, msg);
    }

    public async Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
    {
        var msg = daysLeft <= 1
            ? $"🚨 ayarlıyo - Aboneliğiniz Yarın Sona Eriyor!\n\nMerhaba {salonName}!\n\nAboneliğiniz yarın sona erecek. 😟\n\nKesintisiz hizmet için aboneliğinizi hemen yenileyin.\n\n👉 app.ayarliyo.com/payment"
            : $"⏰ ayarlıyo - Aboneliğiniz {daysLeft} Gün İçinde Sona Eriyor\n\nMerhaba {salonName}!\n\nAboneliğinizin sona ermesine {daysLeft} gün kaldı.\n\nHizmet kesintisi yaşamamak için aboneliğinizi önceden yenileyin.\n\n👉 app.ayarliyo.com/payment";

        await SendTextAsync(phone, msg);
    }

    public async Task SendCustomMessageAsync(string phone, string message)
        => await SendTextAsync(phone, message);

    // ── Meta Graph API gönderim yardımcıları ─────────────────────────────────

    private async Task SendTemplateAsync(string phone, string templateKey, params string[] parameters)
    {
        if (!_settings.Templates.TryGetValue(templateKey, out var templateName))
            throw new InvalidOperationException($"Şablon adı bulunamadı: {templateKey}");

        var bodyParams = parameters.Select(p => new { type = "text", text = p }).ToArray();

        var payload = new
        {
            messaging_product = "whatsapp",
            to = FormatPhone(phone),
            type = "template",
            template = new
            {
                name = templateName,
                language = new { code = _settings.TemplateLanguage },
                components = new[]
                {
                    new { type = "body", parameters = (object)bodyParams }
                }
            }
        };

        await PostAsync(payload);
    }

    private async Task SendTextAsync(string phone, string body)
    {
        var payload = new
        {
            messaging_product = "whatsapp",
            to   = FormatPhone(phone),
            type = "text",
            text = new { body }
        };

        await PostAsync(payload);
    }

    private async Task PostAsync(object payload)
    {
        var url  = $"https://graph.facebook.com/{_settings.ApiVersion}/{_settings.PhoneNumberId}/messages";
        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await _http.PostAsync(url, content);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"Meta API hatası ({(int)response.StatusCode}): {body}");
        }
    }

    // ── Yardımcılar ───────────────────────────────────────────────────────────

    private static string FormatPhone(string phone)
    {
        // E.164 formatı (+ olmadan): 05551234567 → 905551234567
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        if (phone.StartsWith("0"))       return "90" + phone[1..];
        if (!phone.StartsWith("90"))     return "90" + phone;
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
