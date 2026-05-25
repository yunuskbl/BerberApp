using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace BerberApp.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly string _fromNumber;

    public WhatsAppService(IConfiguration config)
    {
        var accountSid = config["Twilio:AccountSid"]!;
        var authToken = config["Twilio:AuthToken"]!;
        _fromNumber = config["Twilio:FromNumber"]!;

        TwilioClient.Init(accountSid, authToken);
    }

    public async Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var mapsLine    = string.IsNullOrWhiteSpace(mapsUrl)    ? "" : $"\n\n📍 *Yol Tarifi için tıklayın:*\n{mapsUrl}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $"\n\n🔗 *Yeni randevu almak için:*\n{bookingUrl}";

        var message = $"""
        ✂ *ayarlıyo - Randevu Onayı*

        Merhaba {customerName}! 👋

        Randevunuz başarıyla oluşturuldu.

        📅 Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)}
        ⏰ Saat: {turkeyTime:HH:mm}
        💈 Hizmet: {serviceName}
        👤 Personel: {staffName}{salonLine}{mapsLine}{bookingLine}

        Randevunuzu iptal etmek için salonumuzu arayabilirsiniz.
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendAppointmentReminderAsync(
        string phone, string customerName,
        string serviceName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var mapsLine    = string.IsNullOrWhiteSpace(mapsUrl)    ? "" : $"\n\n📍 *Yol Tarifi için tıklayın:*\n{mapsUrl}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $"\n\n🔗 *Yeni randevu almak için:*\n{bookingUrl}";

        var message = $"""
        ✂ *ayarlıyo - Randevu Hatırlatması*

        Merhaba {customerName}! 👋

        Yarın randevunuz var!

        📅 Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)}
        ⏰ Saat: {turkeyTime:HH:mm}
        💈 Hizmet: {serviceName}{salonLine}{mapsLine}{bookingLine}

        Sizi bekliyoruz! 😊
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendAppointmentReminder1hAsync(
        string phone, string customerName,
        string serviceName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var mapsLine    = string.IsNullOrWhiteSpace(mapsUrl)    ? "" : $"\n\n📍 *Yol Tarifi için tıklayın:*\n{mapsUrl}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $"\n\n🔗 *Yeni randevu almak için:*\n{bookingUrl}";

        var message = $"""
        ⏰ *ayarlıyo - Randevunuz 1 Saat Sonra!*

        Merhaba {customerName}! 👋

        Randevunuz yaklaşıyor, sizi bekliyoruz!

        📅 Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)}
        ⏰ Saat: {turkeyTime:HH:mm}
        💈 Hizmet: {serviceName}{salonLine}{mapsLine}{bookingLine}

        Görüşmek üzere! 😊
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? " Yeni randevu için salonumuzu arayabilirsiniz." : $"\n\n🔗 *Yeni randevu almak için:*\n{bookingUrl}";

        var message = $"""
        ✂ *ayarlıyo - Randevu İptali*

        Merhaba {customerName},{salonLine}

        {turkeyTime.ToString("dd MMMM yyyy", culture)} tarihli {turkeyTime:HH:mm} saatindeki randevunuz iptal edilmiştir.{bookingLine}
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonLine   = string.IsNullOrWhiteSpace(salonName)  ? "" : $"\n🏪 Salon: {salonName}";
        var bookingLine = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $"\n\n🔗 *Yeni randevu almak için:*\n{bookingUrl}";

        var message = $"""
        ✂ *ayarlıyo - Randevu Güncellendi*

        Merhaba {customerName}! 👋

        Randevunuz güncellendi. Yeni bilgiler:

        📅 Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)}
        ⏰ Saat: {turkeyTime:HH:mm}
        💈 Hizmet: {serviceName}
        👤 Personel: {staffName}{salonLine}{bookingLine}

        Sorularınız için salonumuzu arayabilirsiniz.
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendCustomMessageAsync(string phone, string message)
        => await SendMessageAsync(phone, message);

    private async Task SendMessageAsync(string phone, string message)
    {
        // Telefon numarasını WhatsApp formatına çevir
        var toNumber = $"whatsapp:{FormatPhone(phone)}";

        await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_fromNumber),
            to: new Twilio.Types.PhoneNumber(toNumber)
        );
    }

    private static string FormatPhone(string phone)
    {
        // Türkiye numarası formatla: 05551234567 → +905551234567
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (phone.StartsWith("0"))
            phone = "+90" + phone[1..];
        else if (!phone.StartsWith("+"))
            phone = "+90" + phone;
        return phone;
    }
    public async Task SendOtpAsync(string phone, string otp)
    {
        var message = $"""
        🔐 *ayarlıyo - Doğrulama Kodu*

        Doğrulama kodunuz: *{otp}*

        Bu kod 5 dakika geçerlidir.
        Kodu kimseyle paylaşmayın.
        """;

        await SendMessageAsync(phone, message);
    }
    public async Task SendNewAppointmentRequestAsync(
    string staffPhone, string customerName,
    string customerPhone, string serviceName, DateTime startTime, int sequenceNumber)
    {
        var culture = new System.Globalization.CultureInfo("tr-TR");

        var message = $"""
    🔔 *ayarlıyo - Yeni Randevu Talebi! (#{sequenceNumber})*
    
    👤 Müşteri: {customerName}
    📞 Telefon: {customerPhone}
    🔧 Hizmet: {serviceName}
    📅 Tarih: {startTime.ToString("dd MMMM yyyy", culture)}
    ⏰ Saat: {startTime:HH:mm}
    ✅ Onaylamak için yanıtlayın:
    ONAYLA {sequenceNumber}
    REDDETMEK için yanıtlayın:
    REDDET {sequenceNumber}
    """;

        await SendMessageAsync(staffPhone, message);
    }

    public async Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName, string salonName, string reviewUrl)
    {
        var salonLine = string.IsNullOrWhiteSpace(salonName) ? "" : $"\n🏪 Salon: {salonName}";

        var message = $"""
        ✂ *ayarlıyo - Ziyaretiniz Tamamlandı!*

        Merhaba {customerName}! 👋

        {serviceName} hizmetinden yararlandığınız için teşekkürler! 😊{salonLine}

        Deneyiminizi değerlendirir misiniz? Görüşleriniz bizim için çok önemli.

        ⭐ *Puan vermek için tıklayın:*
        {reviewUrl}

        Teşekkürler! 🙏
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendMonthlyLimitWarningAsync(string phone, string salonName, int currentCount, int limit, bool isFull)
    {
        var message = isFull
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

        await SendMessageAsync(phone, message);
    }

    public async Task SendSubscriptionExpiryWarningAsync(string phone, string salonName, int daysLeft)
    {
        var message = daysLeft <= 1
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

        await SendMessageAsync(phone, message);
    }

    private static DateTime ToTurkeyTime(DateTime utcTime)
    {
        // Kind'ı UTC olarak zorla
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);

        try
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        }
        catch
        {
            var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
            return TimeZoneInfo.ConvertTimeFromUtc(utcTime, tz);
        }
    }
}