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
        string staffName, DateTime startTime, string salonName = "", string address = "")
    {
        var turkeyTime = ToTurkeyTime(startTime);
        var culture    = new System.Globalization.CultureInfo("tr-TR");
        var salonLine  = string.IsNullOrWhiteSpace(salonName) ? "" : $"\n🏪 Salon: {salonName}";
        var mapsLine   = string.IsNullOrWhiteSpace(address)   ? "" :
            $"\n\n📍 *Yol Tarifi:*\nhttps://maps.google.com/?q={Uri.EscapeDataString(address)}";

        var message = $"""
        ✂ *ayarlıyo - Randevu Onayı*

        Merhaba {customerName}! 👋

        Randevunuz başarıyla oluşturuldu.

        📅 Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)}
        ⏰ Saat: {turkeyTime:HH:mm}
        💈 Hizmet: {serviceName}
        👤 Personel: {staffName}{salonLine}{mapsLine}

        Randevunuzu iptal etmek için salonumuzu arayabilirsiniz.
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendAppointmentReminderAsync(
        string phone, string customerName,
        string serviceName, DateTime startTime, string salonName = "", string address = "")
    {
        var turkeyTime = ToTurkeyTime(startTime);
        var culture    = new System.Globalization.CultureInfo("tr-TR");
        var salonLine  = string.IsNullOrWhiteSpace(salonName) ? "" : $"\n🏪 Salon: {salonName}";
        var mapsLine   = string.IsNullOrWhiteSpace(address)   ? "" :
            $"\n\n📍 *Yol Tarifi:*\nhttps://maps.google.com/?q={Uri.EscapeDataString(address)}";

        var message = $"""
        ✂ *ayarlıyo - Randevu Hatırlatması*

        Merhaba {customerName}! 👋

        Yarın randevunuz var!

        📅 Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)}
        ⏰ Saat: {turkeyTime:HH:mm}
        💈 Hizmet: {serviceName}{salonLine}{mapsLine}

        Sizi bekliyoruz! 😊
        """;

        await SendMessageAsync(phone, message);
    }

    public async Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "")
    {
        var turkeyTime = ToTurkeyTime(startTime);
        var culture = new System.Globalization.CultureInfo("tr-TR");
        var salonLine = string.IsNullOrWhiteSpace(salonName) ? "" : $"\n🏪 Salon: {salonName}";

        var message = $"""
        ✂ *ayarlıyo - Randevu İptali*

        Merhaba {customerName},{salonLine}

        {turkeyTime.ToString("dd MMMM yyyy", culture)} tarihli {turkeyTime:HH:mm} saatindeki randevunuz iptal edilmiştir.

        Yeni randevu almak için salonumuzu arayabilirsiniz.
        """;

        await SendMessageAsync(phone, message);
    }

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