using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BerberApp.Infrastructure.Services;

/// <summary>
/// IWhatsAppService → WPPConnect REST API üzerinden serbest metin mesajı gönderir.
/// Meta Business onayı alındığında WhatsAppService'e (Meta Graph API) geçmek için
/// sadece DI kaydını değiştirmek yeterli; bu sınıfa dokunmaya gerek kalmaz.
/// </summary>
public class WppConnectWhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly WppConnectSettings _cfg;
    private readonly ILogger<WppConnectWhatsAppService> _log;
    private readonly System.Globalization.CultureInfo _tr = new("tr-TR");

    public WppConnectWhatsAppService(
        HttpClient http,
        IOptions<WppConnectSettings> options,
        ILogger<WppConnectWhatsAppService> logger)
    {
        _http = http;
        _cfg  = options.Value;
        _log  = logger;
    }

    // ── IWhatsAppService ────────────────────────────────────────────────────

    public Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var t = ToTr(startTime);
        var sb = new StringBuilder();
        sb.AppendLine("✅ *Randevunuz Onaylandı!*");
        sb.AppendLine();
        sb.AppendLine($"Merhaba *{customerName}*! 👋");
        sb.AppendLine();
        sb.AppendLine($"📅 *Tarih:* {t:dd MMMM yyyy}");
        sb.AppendLine($"⏰ *Saat:* {t:HH:mm}");
        sb.AppendLine($"✂️ *Hizmet:* {serviceName}");
        sb.AppendLine($"👤 *Personel:* {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName))
            sb.AppendLine($"🏪 *Salon:* {salonName}");
        if (!string.IsNullOrWhiteSpace(mapsUrl) && mapsUrl != "—")
            sb.AppendLine($"\n📍 Yol tarifi: {mapsUrl}");
        if (!string.IsNullOrWhiteSpace(bookingUrl) && bookingUrl != "—")
            sb.AppendLine($"📋 Randevularım: {bookingUrl}");

        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var t = ToTr(startTime);
        var sb = new StringBuilder();
        sb.AppendLine("🔔 *Randevu Hatırlatması*");
        sb.AppendLine();
        sb.AppendLine($"Merhaba *{customerName}*!");
        sb.AppendLine($"Yarınki randevunuzu hatırlatmak istedik. 😊");
        sb.AppendLine();
        sb.AppendLine($"📅 *Tarih:* {t:dd MMMM yyyy}");
        sb.AppendLine($"⏰ *Saat:* {t:HH:mm}");
        sb.AppendLine($"✂️ *Hizmet:* {serviceName}");
        if (!string.IsNullOrWhiteSpace(staffName))
            sb.AppendLine($"👤 *Personel:* {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName))
            sb.AppendLine($"🏪 *Salon:* {salonName}");
        if (!string.IsNullOrWhiteSpace(mapsUrl) && mapsUrl != "—")
            sb.AppendLine($"\n📍 Yol tarifi: {mapsUrl}");

        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var t = ToTr(startTime);
        var sb = new StringBuilder();
        sb.AppendLine("⏰ *1 Saat Sonra Randevunuz Var!*");
        sb.AppendLine();
        sb.AppendLine($"Merhaba *{customerName}*!");
        sb.AppendLine();
        sb.AppendLine($"🕐 *Saat:* {t:HH:mm}");
        sb.AppendLine($"✂️ *Hizmet:* {serviceName}");
        if (!string.IsNullOrWhiteSpace(staffName))
            sb.AppendLine($"👤 *Personel:* {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName))
            sb.AppendLine($"🏪 *Salon:* {salonName}");
        if (!string.IsNullOrWhiteSpace(mapsUrl) && mapsUrl != "—")
            sb.AppendLine($"📍 {mapsUrl}");

        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
    {
        var t = ToTr(startTime);
        var sb = new StringBuilder();
        sb.AppendLine("❌ *Randevunuz İptal Edildi*");
        sb.AppendLine();
        sb.AppendLine($"Merhaba *{customerName}*,");
        sb.AppendLine($"{t:dd MMMM yyyy} tarihli {t:HH:mm} saatindeki randevunuz iptal edildi.");
        if (!string.IsNullOrWhiteSpace(bookingUrl) && bookingUrl != "—")
            sb.AppendLine($"\nYeni randevu almak için: {bookingUrl}");

        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
    {
        var t = ToTr(startTime);
        var sb = new StringBuilder();
        sb.AppendLine("🔄 *Randevunuz Güncellendi*");
        sb.AppendLine();
        sb.AppendLine($"Merhaba *{customerName}*! Randevunuz aşağıdaki şekilde güncellendi:");
        sb.AppendLine();
        sb.AppendLine($"📅 *Yeni Tarih:* {t:dd MMMM yyyy}");
        sb.AppendLine($"⏰ *Yeni Saat:* {t:HH:mm}");
        sb.AppendLine($"✂️ *Hizmet:* {serviceName}");
        sb.AppendLine($"👤 *Personel:* {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName))
            sb.AppendLine($"🏪 *Salon:* {salonName}");

        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName,
        string salonName, string reviewUrl)
    {
        var sb = new StringBuilder();
        sb.AppendLine("🙏 *Hizmetimizi Tercih Ettiğiniz İçin Teşekkürler!*");
        sb.AppendLine();
        sb.AppendLine($"Merhaba *{customerName}*,");
        sb.AppendLine($"_{serviceName}_ hizmetimizi aldığınız için teşekkür ederiz.");
        if (!string.IsNullOrWhiteSpace(salonName))
            sb.AppendLine($"Sizi *{salonName}*'da görmek güzeldi! 💈");
        if (!string.IsNullOrWhiteSpace(reviewUrl) && reviewUrl != "—")
        {
            sb.AppendLine();
            sb.AppendLine($"⭐ Deneyiminizi değerlendirmek ister misiniz?");
            sb.AppendLine(reviewUrl);
        }

        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendNewAppointmentRequestAsync(
        string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTime, int sequenceNumber)
    {
        var t = ToTr(startTime);
        var sb = new StringBuilder();
        sb.AppendLine($"📬 *Yeni Randevu Talebi #{sequenceNumber}*");
        sb.AppendLine();
        sb.AppendLine($"👤 *Müşteri:* {customerName}");
        sb.AppendLine($"📞 *Telefon:* {customerPhone}");
        sb.AppendLine($"✂️ *Hizmet:* {serviceName}");
        sb.AppendLine($"📅 *Tarih:* {t:dd MMMM yyyy}");
        sb.AppendLine($"⏰ *Saat:* {t:HH:mm}");
        sb.AppendLine();
        sb.AppendLine("Randevuyu onaylamak için panel üzerinden işlem yapabilirsiniz.");

        return SendTextAsync(staffPhone, sb.ToString().TrimEnd());
    }

    public Task SendOtpAsync(string phone, string otp)
    {
        var msg = $"🔐 *ayarlıyo doğrulama kodunuz:*\n\n*{otp}*\n\nBu kodu kimseyle paylaşmayın.";
        return SendTextAsync(phone, msg);
    }

    public Task SendMonthlyLimitWarningAsync(
        string phone, string salonName, int currentCount, int limit, bool isFull)
    {
        var msg = isFull
            ? $"⚠️ *ayarlıyo - Aylık Randevu Limitiniz Doldu!*\n\nMerhaba *{salonName}*!\n\nBu ay {limit} randevu limitinize ulaştınız. 🚫\n\nYeni randevular bu ay alınamayacak. Limitinizi artırmak için planınızı yükseltin.\n\n👉 app.ayarliyo.com/pricing"
            : $"⚠️ *ayarlıyo - Aylık Randevu Limitine Yaklaşıyorsunuz!*\n\nMerhaba *{salonName}*!\n\nBu ay {currentCount}/{limit} randevu kullandınız (%80).\n\nKesintisiz hizmet için planınızı yükseltmeyi düşünün.\n\n👉 app.ayarliyo.com/pricing";

        return SendTextAsync(phone, msg);
    }

    public Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
    {
        var msg = daysLeft <= 1
            ? $"🚨 *ayarlıyo - Aboneliğiniz Yarın Sona Eriyor!*\n\nMerhaba *{salonName}*!\n\nKesintisiz hizmet için aboneliğinizi hemen yenileyin.\n\n👉 app.ayarliyo.com/payment"
            : $"⏰ *ayarlıyo - Aboneliğiniz {daysLeft} Gün İçinde Sona Eriyor*\n\nMerhaba *{salonName}*!\n\nHizmet kesintisi yaşamamak için aboneliğinizi önceden yenileyin.\n\n👉 app.ayarliyo.com/payment";

        return SendTextAsync(phone, msg);
    }

    public async Task SendCustomMessageAsync(string phone, string message, string? imageUrl = null)
    {
        // WPPConnect görsel gönderimini destekler; imageUrl varsa önce görseli gönderir
        if (!string.IsNullOrWhiteSpace(imageUrl))
        {
            await SendImageAsync(phone, imageUrl, message);
        }
        else
        {
            await SendTextAsync(phone, message);
        }
    }

    // ── WPPConnect REST API çağrıları ────────────────────────────────────────

    private async Task SendTextAsync(string phone, string message)
    {
        var payload = new
        {
            phone   = FormatPhone(phone),
            message,
            isGroup = false
        };
        await PostAsync("send-message", payload);
    }

    private async Task SendImageAsync(string phone, string imageUrl, string caption)
    {
        var payload = new
        {
            phone   = FormatPhone(phone),
            path    = imageUrl,
            caption,
            isGroup = false
        };
        await PostAsync("send-image", payload);
    }

    private async Task PostAsync(string endpoint, object payload)
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
        {
            _log.LogWarning("[WPPConnect] Token yapılandırılmamış. Mesaj gönderilemedi.");
            return;
        }

        var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{endpoint}";
        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.Token);
        request.Content = new StringContent(json, Encoding.UTF8, "application/json");

        _log.LogInformation("[WPPConnect] POST → {Url}", url);

        try
        {
            var response = await _http.SendAsync(request);
            var body     = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
                _log.LogInformation("[WPPConnect] OK {Status}", (int)response.StatusCode);
            else
                _log.LogError("[WPPConnect] Hata {Status}: {Body}", (int)response.StatusCode, body);
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[WPPConnect] İstek başarısız: {Url}", url);
        }
    }

    // ── Yardımcılar ──────────────────────────────────────────────────────────

    /// <summary>WPPConnect bireysel chat formatı: 905551234567@c.us</summary>
    private static string FormatPhone(string phone)
    {
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        if (phone.StartsWith("0")) phone = "90" + phone[1..];
        else if (!phone.StartsWith("90")) phone = "90" + phone;

        // @c.us ekli değilse ekle
        if (!phone.Contains('@')) phone += "@c.us";
        return phone;
    }

    private static DateTime ToTr(DateTime utcTime)
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
