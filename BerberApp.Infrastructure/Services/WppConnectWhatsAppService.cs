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
    private readonly IOptionsMonitor<WppConnectSettings>? _cfgMonitor;
    private readonly WppConnectSettings? _fixedCfg;
    private WppConnectSettings _cfg => _fixedCfg ?? _cfgMonitor!.CurrentValue;
    private readonly ILogger<WppConnectWhatsAppService> _log;
    private readonly ISmsService? _smsFallback;

    private enum Lang { TR, EN, RU, DE }

    private static readonly CultureInfo CtrTR = new("tr-TR");
    private static readonly CultureInfo CtrEN = new("en-US");
    private static readonly CultureInfo CtrRU = new("ru-RU");
    private static readonly CultureInfo CtrDE = new("de-DE");

    public WppConnectWhatsAppService(
        HttpClient http,
        IOptionsMonitor<WppConnectSettings> options,
        ILogger<WppConnectWhatsAppService> logger,
        ISmsService? smsFallback = null)
    {
        _http        = http;
        _cfgMonitor  = options;
        _log         = logger;
        _smsFallback = smsFallback;
    }

    private WppConnectWhatsAppService(HttpClient http, WppConnectSettings cfg, ILogger<WppConnectWhatsAppService> logger, ISmsService? smsFallback)
    {
        _http        = http;
        _fixedCfg    = cfg;
        _log         = logger;
        _smsFallback = smsFallback;
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
        return new WppConnectWhatsAppService(_http, cfg, _log, _smsFallback);
    }

    // ── IWhatsAppService ─────────────────────────────────────────────────────

    public async Task SendAppointmentConfirmedAsync(
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

        var sent = await TrySendTextAsync(phone, sb.ToString().TrimEnd());
        if (!sent)
            await (_smsFallback?.SendAppointmentConfirmedAsync(phone, customerName, serviceName, staffName, startTime, salonName, mapsUrl, bookingUrl) ?? Task.CompletedTask);
    }

    public async Task SendAppointmentReminderAsync(
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

        var sent = await TrySendTextAsync(phone, sb.ToString().TrimEnd());
        if (!sent)
            await (_smsFallback?.SendAppointmentReminderAsync(phone, customerName, serviceName, startTime, salonName, mapsUrl, bookingUrl, staffName) ?? Task.CompletedTask);
    }

    public async Task SendAppointmentReminder1hAsync(
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

        var sent = await TrySendTextAsync(phone, sb.ToString().TrimEnd());
        if (!sent)
            await (_smsFallback?.SendAppointmentReminder1hAsync(phone, customerName, serviceName, startTime, salonName, mapsUrl, bookingUrl, staffName) ?? Task.CompletedTask);
    }

    public async Task SendAppointmentCancelledAsync(
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

        var sent = await TrySendTextAsync(phone, sb.ToString().TrimEnd());
        if (!sent)
            await (_smsFallback?.SendAppointmentCancelledAsync(phone, customerName, startTime, salonName, bookingUrl) ?? Task.CompletedTask);
    }

    public async Task SendAppointmentUpdatedAsync(
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

        var sent = await TrySendTextAsync(phone, sb.ToString().TrimEnd());
        if (!sent)
            await (_smsFallback?.SendAppointmentUpdatedAsync(phone, customerName, serviceName, staffName, startTime, salonName, bookingUrl) ?? Task.CompletedTask);
    }

    public async Task SendAppointmentCompletedAsync(
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

        var sent = await TrySendTextAsync(phone, sb.ToString().TrimEnd());
        if (!sent)
            await (_smsFallback?.SendAppointmentCompletedAsync(phone, customerName, serviceName, salonName, reviewUrl) ?? Task.CompletedTask);
    }

    public Task SendNewAppointmentRequestAsync(
        string staffPhone, string customerName, string customerPhone,
        string serviceName, DateTime startTime, int sequenceNumber)
    {
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
        return TrySendTextAsync(staffPhone, msg);
    }

    public async Task SendOtpAsync(string phone, string otp)
    {
        var msg = DetectLang(phone) switch
        {
            Lang.RU => $"🔐 *Ваш код подтверждения ayarlıyo:*\n\n*{otp}*\n\nНе сообщайте этот код никому.",
            Lang.DE => $"🔐 *Ihr ayarlıyo Bestätigungscode:*\n\n*{otp}*\n\nTeilen Sie diesen Code nicht mit anderen.",
            Lang.EN => $"🔐 *Your ayarlıyo verification code:*\n\n*{otp}*\n\nDo not share this code with anyone.",
            _        => $"🔐 *ayarlıyo doğrulama kodunuz:*\n\n*{otp}*\n\nBu kodu kimseyle paylaşmayın.",
        };

        var sent = await TrySendTextAsync(phone, msg);
        if (!sent)
            await (_smsFallback?.SendOtpAsync(phone, otp) ?? Task.CompletedTask);
    }

    public Task SendMonthlyLimitWarningAsync(
        string phone, string salonName, int currentCount, int limit, bool isFull)
    {
        var msg = isFull
            ? $"⚠️ *ayarlıyo - Aylık Randevu Limitiniz Doldu!*\n\nMerhaba *{salonName}*!\n\nBu ay {limit} randevu limitinize ulaştınız. 🚫\n\nYeni randevular bu ay alınamayacak. Limit artırmak için planınızı yükseltin.\n\n👉 app.ayarliyo.com/pricing"
            : $"⚠️ *ayarlıyo - Aylık Randevu Limitine Yaklaşıyorsunuz!*\n\nMerhaba *{salonName}*!\n\nBu ay {currentCount}/{limit} randevu kullandınız (%80).\n\nKesintisiz hizmet için planınızı yükseltmeyi düşünün.\n\n👉 app.ayarliyo.com/pricing";
        return TrySendTextAsync(phone, msg);
    }

    public Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
    {
        var msg = daysLeft <= 1
            ? $"🚨 *ayarlıyo - Aboneliğiniz Yarın Sona Eriyor!*\n\nMerhaba *{salonName}*!\n\nKesintisiz hizmet için aboneliğinizi hemen yenileyin.\n\n👉 app.ayarliyo.com/payment"
            : $"⏰ *ayarlıyo - Aboneliğiniz {daysLeft} Gün İçinde Sona Eriyor*\n\nMerhaba *{salonName}*!\n\nHizmet kesintisi yaşamamak için aboneliğinizi önceden yenileyin.\n\n👉 app.ayarliyo.com/payment";
        return TrySendTextAsync(phone, msg);
    }

    public async Task SendCustomMessageAsync(string phone, string message, string? imageUrl = null)
    {
        if (!string.IsNullOrWhiteSpace(imageUrl))
            await TrySendImageAsync(phone, imageUrl, message);
        else
            await TrySendTextAsync(phone, message);
    }

    // ── WPPConnect REST API ──────────────────────────────────────────────────

    private async Task<bool> TrySendTextAsync(string phone, string message)
    {
        var payload = new { phone = FormatPhone(phone), message, isGroup = false };
        return await PostAsync("send-message", payload);
    }

    private async Task<bool> TrySendImageAsync(string phone, string imageUrl, string caption)
    {
        var payload = new { phone = FormatPhone(phone), path = imageUrl, caption, isGroup = false };
        return await PostAsync("send-image", payload);
    }

    private const string PersistentTokenPath = "/app/wwwroot/uploads/.wppconnect.token";

    private async Task<string?> ResolveTokenAsync()
    {
        try
        {
            if (File.Exists(PersistentTokenPath))
            {
                var t = (await File.ReadAllTextAsync(PersistentTokenPath)).Trim();
                if (!string.IsNullOrWhiteSpace(t)) return t;
            }
        }
        catch { }
        return string.IsNullOrWhiteSpace(_cfg.Token) ? null : _cfg.Token;
    }

    private async Task<bool> PostAsync(string endpoint, object payload)
    {
        var token = await ResolveTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
        {
            _log.LogWarning("[WPPConnect] Token yapılandırılmamış. Mesaj gönderilemedi.");
            return false;
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
            {
                var delivered = !IsSendFailure(body);
                _log.LogInformation("[WPPConnect] OK {Status} delivered={Delivered}: {Body}", (int)response.StatusCode, delivered, body);
                return delivered;
            }
            else
            {
                _log.LogError("[WPPConnect] Hata {Status}: {Body}", (int)response.StatusCode, body);
                return false;
            }
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "[WPPConnect] İstek başarısız: {Url}", url);
            return false;
        }
    }

    private static bool IsSendFailure(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("response", out var arr) &&
                arr.ValueKind == JsonValueKind.Array &&
                arr.GetArrayLength() > 0)
            {
                var first = arr[0];
                if (first.TryGetProperty("isSendFailure", out var fail) && fail.GetBoolean())
                    return true;
                if (first.TryGetProperty("ack", out var ack) && ack.GetInt32() == -1)
                    return true;
            }
        }
        catch { }
        return false;
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

    private static string FormatPhone(string phone)
    {
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");
        if (phone.StartsWith("0"))  phone = "90" + phone[1..];
        else if (phone.Length == 10) phone = "90" + phone;
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
