using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BerberApp.Infrastructure.Services;

public class WppConnectWhatsAppService : IWhatsAppService
{
    private readonly HttpClient _http;
    private readonly WppConnectSettings _cfg;
    private readonly ILogger<WppConnectWhatsAppService> _log;

    private enum Lang { TR, EN, RU, DE }

    private static readonly CultureInfo CtrTR = new("tr-TR");
    private static readonly CultureInfo CtrEN = new("en-US");
    private static readonly CultureInfo CtrRU = new("ru-RU");
    private static readonly CultureInfo CtrDE = new("de-DE");

    public WppConnectWhatsAppService(
        HttpClient http,
        IOptions<WppConnectSettings> options,
        ILogger<WppConnectWhatsAppService> logger)
    {
        _http = http;
        _cfg  = options.Value;
        _log  = logger;
    }

    private WppConnectWhatsAppService(HttpClient http, WppConnectSettings cfg, ILogger<WppConnectWhatsAppService> logger)
    {
        _http = http;
        _cfg  = cfg;
        _log  = logger;
    }

    public IWhatsAppService ForTenant(string? session, string? token)
    {
        if (string.IsNullOrWhiteSpace(session) || string.IsNullOrWhiteSpace(token))
            return this;
        var cfg = new WppConnectSettings
        {
            BaseUrl   = _cfg.BaseUrl,
            SecretKey = _cfg.SecretKey,
            Session   = session,
            Token     = token,
        };
        return new WppConnectWhatsAppService(_http, cfg, _log);
    }

    // ── IWhatsAppService ─────────────────────────────────────────────────────

    public Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var lang = DetectLang(phone);
        var ci   = CultureFor(lang);
        var t    = ToTr(startTime);
        var date = t.ToString("dd MMMM yyyy", ci);
        var time = t.ToString("HH:mm");

        var (title, greeting, lDate, lTime, lService, lStaff, lSalon, lMaps, lBooking) = lang switch
        {
            Lang.RU => ("✅ *Ваша запись подтверждена!*",
                        $"Здравствуйте, *{customerName}*! 👋",
                        "📅 *Дата:*", "⏰ *Время:*", "✂️ *Услуга:*",
                        "👤 *Мастер:*", "🏪 *Салон:*",
                        "📍 Маршрут:", "📋 Мои записи:"),
            Lang.DE => ("✅ *Ihr Termin ist bestätigt!*",
                        $"Hallo *{customerName}*! 👋",
                        "📅 *Datum:*", "⏰ *Uhrzeit:*", "✂️ *Service:*",
                        "👤 *Mitarbeiter:*", "🏪 *Salon:*",
                        "📍 Route:", "📋 Meine Termine:"),
            Lang.EN => ("✅ *Your Appointment is Confirmed!*",
                        $"Hello *{customerName}*! 👋",
                        "📅 *Date:*", "⏰ *Time:*", "✂️ *Service:*",
                        "👤 *Staff:*", "🏪 *Salon:*",
                        "📍 Directions:", "📋 My Appointments:"),
            _ =>       ("✅ *Randevunuz Onaylandı!*",
                        $"Merhaba *{customerName}*! 👋",
                        "📅 *Tarih:*", "⏰ *Saat:*", "✂️ *Hizmet:*",
                        "👤 *Personel:*", "🏪 *Salon:*",
                        "📍 Yol tarifi:", "📋 Randevularım:"),
        };

        var sb = new StringBuilder();
        sb.AppendLine(title).AppendLine();
        sb.AppendLine(greeting).AppendLine();
        sb.AppendLine($"{lDate} {date}");
        sb.AppendLine($"{lTime} {time}");
        sb.AppendLine($"{lService} {serviceName}");
        sb.AppendLine($"{lStaff} {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName)) sb.AppendLine($"{lSalon} {salonName}");
        if (!string.IsNullOrWhiteSpace(mapsUrl) && mapsUrl != "—") sb.AppendLine($"\n{lMaps} {mapsUrl}");
        if (!string.IsNullOrWhiteSpace(bookingUrl) && bookingUrl != "—") sb.AppendLine($"{lBooking} {bookingUrl}");
        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var lang = DetectLang(phone);
        var ci   = CultureFor(lang);
        var t    = ToTr(startTime);
        var date = t.ToString("dd MMMM yyyy", ci);
        var time = t.ToString("HH:mm");

        var (title, body, lDate, lTime, lService, lStaff, lSalon, lMaps) = lang switch
        {
            Lang.RU => ("🔔 *Напоминание о записи*",
                        $"Здравствуйте, *{customerName}*!\nНапоминаем о вашей записи завтра. 😊",
                        "📅 *Дата:*", "⏰ *Время:*", "✂️ *Услуга:*",
                        "👤 *Мастер:*", "🏪 *Салон:*", "📍 Маршрут:"),
            Lang.DE => ("🔔 *Terminerinnerung*",
                        $"Hallo *{customerName}*!\nErinnerung an Ihren Termin morgen. 😊",
                        "📅 *Datum:*", "⏰ *Uhrzeit:*", "✂️ *Service:*",
                        "👤 *Mitarbeiter:*", "🏪 *Salon:*", "📍 Route:"),
            Lang.EN => ("🔔 *Appointment Reminder*",
                        $"Hello *{customerName}*!\nReminding you of your appointment tomorrow. 😊",
                        "📅 *Date:*", "⏰ *Time:*", "✂️ *Service:*",
                        "👤 *Staff:*", "🏪 *Salon:*", "📍 Directions:"),
            _ =>       ("🔔 *Randevu Hatırlatması*",
                        $"Merhaba *{customerName}*!\nYarınki randevunuzu hatırlatmak istedik. 😊",
                        "📅 *Tarih:*", "⏰ *Saat:*", "✂️ *Hizmet:*",
                        "👤 *Personel:*", "🏪 *Salon:*", "📍 Yol tarifi:"),
        };

        var sb = new StringBuilder();
        sb.AppendLine(title).AppendLine();
        sb.AppendLine(body).AppendLine();
        sb.AppendLine($"{lDate} {date}");
        sb.AppendLine($"{lTime} {time}");
        sb.AppendLine($"{lService} {serviceName}");
        if (!string.IsNullOrWhiteSpace(staffName)) sb.AppendLine($"{lStaff} {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName)) sb.AppendLine($"{lSalon} {salonName}");
        if (!string.IsNullOrWhiteSpace(mapsUrl) && mapsUrl != "—") sb.AppendLine($"\n{lMaps} {mapsUrl}");
        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime,
        string salonName = "", string mapsUrl = "", string bookingUrl = "",
        string staffName = "")
    {
        var lang = DetectLang(phone);
        var t    = ToTr(startTime);
        var time = t.ToString("HH:mm");

        var (title, body, lTime, lService, lStaff, lSalon, lMaps) = lang switch
        {
            Lang.RU => ("⏰ *Ваша запись через 1 час!*",
                        $"Здравствуйте, *{customerName}*!",
                        "🕐 *Время:*", "✂️ *Услуга:*", "👤 *Мастер:*",
                        "🏪 *Салон:*", "📍 Маршрут:"),
            Lang.DE => ("⏰ *Ihr Termin ist in 1 Stunde!*",
                        $"Hallo *{customerName}*!",
                        "🕐 *Uhrzeit:*", "✂️ *Service:*", "👤 *Mitarbeiter:*",
                        "🏪 *Salon:*", "📍 Route:"),
            Lang.EN => ("⏰ *Your Appointment is in 1 Hour!*",
                        $"Hello *{customerName}*!",
                        "🕐 *Time:*", "✂️ *Service:*", "👤 *Staff:*",
                        "🏪 *Salon:*", "📍 Directions:"),
            _ =>       ("⏰ *1 Saat Sonra Randevunuz Var!*",
                        $"Merhaba *{customerName}*!",
                        "🕐 *Saat:*", "✂️ *Hizmet:*", "👤 *Personel:*",
                        "🏪 *Salon:*", "📍 Yol tarifi:"),
        };

        var sb = new StringBuilder();
        sb.AppendLine(title).AppendLine();
        sb.AppendLine(body).AppendLine();
        sb.AppendLine($"{lTime} {time}");
        sb.AppendLine($"{lService} {serviceName}");
        if (!string.IsNullOrWhiteSpace(staffName)) sb.AppendLine($"{lStaff} {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName)) sb.AppendLine($"{lSalon} {salonName}");
        if (!string.IsNullOrWhiteSpace(mapsUrl) && mapsUrl != "—") sb.AppendLine($"{lMaps} {mapsUrl}");
        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
    {
        var lang = DetectLang(phone);
        var ci   = CultureFor(lang);
        var t    = ToTr(startTime);
        var date = t.ToString("dd MMMM yyyy", ci);
        var time = t.ToString("HH:mm");

        var (title, body, lNew) = lang switch
        {
            Lang.RU => ("❌ *Ваша запись отменена*",
                        $"Здравствуйте, *{customerName}*,\nваша запись на {date} в {time} была отменена.",
                        "Записаться снова:"),
            Lang.DE => ("❌ *Ihr Termin wurde storniert*",
                        $"Hallo *{customerName}*,\nIhr Termin am {date} um {time} Uhr wurde storniert.",
                        "Neuen Termin buchen:"),
            Lang.EN => ("❌ *Your Appointment has been Cancelled*",
                        $"Hello *{customerName}*,\nyour appointment on {date} at {time} has been cancelled.",
                        "Book a new appointment:"),
            _ =>       ("❌ *Randevunuz İptal Edildi*",
                        $"Merhaba *{customerName}*,\n{date} tarihli {time} saatindeki randevunuz iptal edildi.",
                        "Yeni randevu almak için:"),
        };

        var sb = new StringBuilder();
        sb.AppendLine(title).AppendLine();
        sb.AppendLine(body);
        if (!string.IsNullOrWhiteSpace(bookingUrl) && bookingUrl != "—")
            sb.AppendLine($"\n{lNew} {bookingUrl}");
        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime,
        string salonName = "", string bookingUrl = "")
    {
        var lang = DetectLang(phone);
        var ci   = CultureFor(lang);
        var t    = ToTr(startTime);
        var date = t.ToString("dd MMMM yyyy", ci);
        var time = t.ToString("HH:mm");

        var (title, body, lDate, lTime, lService, lStaff, lSalon) = lang switch
        {
            Lang.RU => ("🔄 *Ваша запись обновлена*",
                        $"Здравствуйте, *{customerName}*! Ваша запись была изменена:",
                        "📅 *Новая дата:*", "⏰ *Новое время:*", "✂️ *Услуга:*",
                        "👤 *Мастер:*", "🏪 *Салон:*"),
            Lang.DE => ("🔄 *Ihr Termin wurde aktualisiert*",
                        $"Hallo *{customerName}*! Ihr Termin wurde geändert:",
                        "📅 *Neues Datum:*", "⏰ *Neue Uhrzeit:*", "✂️ *Service:*",
                        "👤 *Mitarbeiter:*", "🏪 *Salon:*"),
            Lang.EN => ("🔄 *Your Appointment has been Updated*",
                        $"Hello *{customerName}*! Your appointment has been changed:",
                        "📅 *New Date:*", "⏰ *New Time:*", "✂️ *Service:*",
                        "👤 *Staff:*", "🏪 *Salon:*"),
            _ =>       ("🔄 *Randevunuz Güncellendi*",
                        $"Merhaba *{customerName}*! Randevunuz aşağıdaki şekilde güncellendi:",
                        "📅 *Yeni Tarih:*", "⏰ *Yeni Saat:*", "✂️ *Hizmet:*",
                        "👤 *Personel:*", "🏪 *Salon:*"),
        };

        var lBooking = lang switch
        {
            Lang.RU => "📋 Мои записи:",
            Lang.DE => "📋 Meine Termine:",
            Lang.EN => "📋 My Appointments:",
            _       => "📋 Randevularım:",
        };

        var sb = new StringBuilder();
        sb.AppendLine(title).AppendLine();
        sb.AppendLine(body).AppendLine();
        sb.AppendLine($"{lDate} {date}");
        sb.AppendLine($"{lTime} {time}");
        sb.AppendLine($"{lService} {serviceName}");
        sb.AppendLine($"{lStaff} {staffName}");
        if (!string.IsNullOrWhiteSpace(salonName)) sb.AppendLine($"{lSalon} {salonName}");
        if (!string.IsNullOrWhiteSpace(bookingUrl) && bookingUrl != "—") sb.AppendLine($"{lBooking} {bookingUrl}");
        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName,
        string salonName, string reviewUrl)
    {
        var lang = DetectLang(phone);

        var (title, body, lReview) = lang switch
        {
            Lang.RU => ("🙏 *Спасибо, что выбрали нас!*",
                        $"Здравствуйте, *{customerName}*!\nБлагодарим за посещение _{serviceName}_." +
                        (!string.IsNullOrWhiteSpace(salonName) ? $" Рады были видеть вас в *{salonName}*! 💈" : ""),
                        "⭐ Оцените нас:"),
            Lang.DE => ("🙏 *Vielen Dank für Ihren Besuch!*",
                        $"Hallo *{customerName}*!\nVielen Dank für die Inanspruchnahme von _{serviceName}_." +
                        (!string.IsNullOrWhiteSpace(salonName) ? $" Es war schön, Sie in *{salonName}* zu sehen! 💈" : ""),
                        "⭐ Bewerten Sie uns:"),
            Lang.EN => ("🙏 *Thank You for Choosing Our Service!*",
                        $"Hello *{customerName}*!\nThank you for using _{serviceName}_." +
                        (!string.IsNullOrWhiteSpace(salonName) ? $" It was great to see you at *{salonName}*! 💈" : ""),
                        "⭐ Rate your experience:"),
            _ =>       ("🙏 *Hizmetimizi Tercih Ettiğiniz İçin Teşekkürler!*",
                        $"Merhaba *{customerName}*!\n_{serviceName}_ hizmetimizi aldığınız için teşekkür ederiz." +
                        (!string.IsNullOrWhiteSpace(salonName) ? $" Sizi *{salonName}*'da görmek güzeldi! 💈" : ""),
                        "⭐ Deneyiminizi değerlendirin:"),
        };

        var sb = new StringBuilder();
        sb.AppendLine(title).AppendLine();
        sb.AppendLine(body);
        if (!string.IsNullOrWhiteSpace(reviewUrl) && reviewUrl != "—")
            sb.AppendLine($"\n{lReview}\n{reviewUrl}");
        return SendTextAsync(phone, sb.ToString().TrimEnd());
    }

    public Task SendNewAppointmentRequestAsync(
        string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTime, int sequenceNumber)
    {
        // Personele giden bildirim — her zaman Türkçe
        var t   = ToTr(startTime);
        var msg = new StringBuilder()
            .AppendLine($"📬 *Yeni Randevu Talebi #{sequenceNumber}*").AppendLine()
            .AppendLine($"👤 *Müşteri:* {customerName}")
            .AppendLine($"📞 *Telefon:* {customerPhone}")
            .AppendLine($"✂️ *Hizmet:* {serviceName}")
            .AppendLine($"📅 *Tarih:* {t.ToString("dd MMMM yyyy", CtrTR)}")
            .AppendLine($"⏰ *Saat:* {t:HH:mm}")
            .AppendLine()
            .Append("Onaylamak için panel üzerinden işlem yapabilirsiniz.")
            .ToString();
        return SendTextAsync(staffPhone, msg);
    }

    public Task SendOtpAsync(string phone, string otp)
    {
        var msg = DetectLang(phone) switch
        {
            Lang.RU => $"🔐 *Ваш код подтверждения ayarlıyo:*\n\n*{otp}*\n\nНе сообщайте этот код никому.",
            Lang.DE => $"🔐 *Ihr ayarlıyo Bestätigungscode:*\n\n*{otp}*\n\nTeilen Sie diesen Code nicht mit anderen.",
            Lang.EN => $"🔐 *Your ayarlıyo verification code:*\n\n*{otp}*\n\nDo not share this code with anyone.",
            _        => $"🔐 *ayarlıyo doğrulama kodunuz:*\n\n*{otp}*\n\nBu kodu kimseyle paylaşmayın.",
        };
        return SendTextAsync(phone, msg);
    }

    public Task SendMonthlyLimitWarningAsync(
        string phone, string salonName, int currentCount, int limit, bool isFull)
    {
        // İşletme sahibine giden bildirim — Türkçe
        var msg = isFull
            ? $"⚠️ *ayarlıyo - Aylık Randevu Limitiniz Doldu!*\n\nMerhaba *{salonName}*!\n\nBu ay {limit} randevu limitinize ulaştınız. 🚫\n\nYeni randevular bu ay alınamayacak. Limit artırmak için planınızı yükseltin.\n\n👉 app.ayarliyo.com/pricing"
            : $"⚠️ *ayarlıyo - Aylık Randevu Limitine Yaklaşıyorsunuz!*\n\nMerhaba *{salonName}*!\n\nBu ay {currentCount}/{limit} randevu kullandınız (%80).\n\nKesintisiz hizmet için planınızı yükseltmeyi düşünün.\n\n👉 app.ayarliyo.com/pricing";
        return SendTextAsync(phone, msg);
    }

    public Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
    {
        // İşletme sahibine giden bildirim — Türkçe
        var msg = daysLeft <= 1
            ? $"🚨 *ayarlıyo - Aboneliğiniz Yarın Sona Eriyor!*\n\nMerhaba *{salonName}*!\n\nKesintisiz hizmet için aboneliğinizi hemen yenileyin.\n\n👉 app.ayarliyo.com/payment"
            : $"⏰ *ayarlıyo - Aboneliğiniz {daysLeft} Gün İçinde Sona Eriyor*\n\nMerhaba *{salonName}*!\n\nHizmet kesintisi yaşamamak için aboneliğinizi önceden yenileyin.\n\n👉 app.ayarliyo.com/payment";
        return SendTextAsync(phone, msg);
    }

    public async Task SendCustomMessageAsync(string phone, string message, string? imageUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
            await SendImageAsync(phone, imageUrl, message);
        else
            await SendTextAsync(phone, message);
    }

    // ── WPPConnect REST API ──────────────────────────────────────────────────

    private async Task SendTextAsync(string phone, string message)
    {
        var payload = new { phone = FormatPhone(phone), message, isGroup = false };
        await PostAsync("send-message", payload);
    }

    private async Task SendImageAsync(string phone, string imageUrl, string caption)
    {
        var payload = new { phone = FormatPhone(phone), path = imageUrl, caption, isGroup = false };
        await PostAsync("send-image", payload);
    }

    private const string PersistentTokenPath = "/app/wwwroot/uploads/.wppconnect.token";

    private async Task<string?> ResolveTokenAsync()
    {
        if (!string.IsNullOrWhiteSpace(_cfg.Token))
            return _cfg.Token;
        try
        {
            if (File.Exists(PersistentTokenPath))
            {
                var t = (await File.ReadAllTextAsync(PersistentTokenPath)).Trim();
                if (!string.IsNullOrWhiteSpace(t)) return t;
            }
        }
        catch { /* ignore */ }
        return null;
    }

    private async Task PostAsync(string endpoint, object payload)
    {
        var token = await ResolveTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            _log.LogWarning("[WPPConnect] Token yapılandırılmamış. Mesaj gönderilemedi.");
            return;
        }

        var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{endpoint}";
        var json = JsonSerializer.Serialize(payload);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
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

    private static Lang DetectLang(string phone)
    {
        var p = phone.Replace("+", "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (p.StartsWith("90") || p.StartsWith("0")) return Lang.TR;
        if (p.StartsWith("7"))                        return Lang.RU;
        if (p.StartsWith("49"))                       return Lang.DE;
        return Lang.EN;
    }

    private static CultureInfo CultureFor(Lang lang) => lang switch
    {
        Lang.RU => CtrRU,
        Lang.DE => CtrDE,
        Lang.EN => CtrEN,
        _       => CtrTR,
    };

    /// <summary>WPPConnect bireysel chat formatı: 905551234567@c.us</summary>
    private static string FormatPhone(string phone)
    {
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        // Yerel Türk numarası: 0 ile başlıyorsa veya 10 haneliyse 90 ekle
        if (phone.StartsWith("0"))  phone = "90" + phone[1..];
        else if (phone.Length == 10) phone = "90" + phone;
        // 11+ haneli numaralar zaten ülke koduna sahip (90..., 7..., 49... vb.)
        if (!phone.Contains('@')) phone += "@c.us";
        return phone;
    }

    private static DateTime ToTr(DateTime utcTime)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        try   { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")); }
        catch { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")); }
    }
}
