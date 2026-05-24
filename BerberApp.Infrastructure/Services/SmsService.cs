using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace BerberApp.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly string _fromNumber;

    public SmsService(IConfiguration config)
    {
        var accountSid = config["Twilio:AccountSid"]!;
        var authToken  = config["Twilio:AuthToken"]!;
        _fromNumber    = config["Twilio:SmsFromNumber"]!;

        TwilioClient.Init(accountSid, authToken);
    }

    public async Task SendOtpAsync(string phone, string otp)
    {
        var message = $"ayarlıyo dogrulama kodunuz: {otp}. Bu kod 5 dakika gecerlidir.";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" Salon: {salonName}.";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var message     = $"ayarliyo: Randevunuz onaylandi! " +
                          $"Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm}, " +
                          $"Hizmet: {serviceName}, Personel: {staffName}.{salonPart}{bookingPart}";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var message     = $"ayarliyo: Yarin randevunuz var! " +
                          $"{turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} - {serviceName}.{bookingPart}";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var message     = $"ayarliyo: Randevunuz 1 saat sonra! " +
                          $"{turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} - {serviceName}.{bookingPart}";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" ({salonName})";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Yeni randevu: {bookingUrl}";
        var message     = $"ayarliyo{salonPart}: " +
                          $"{turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} " +
                          $"tarihli randevunuz iptal edildi.{bookingPart}";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName, string salonName, string reviewUrl)
    {
        var salonPart = string.IsNullOrWhiteSpace(salonName) ? "" : $" ({salonName})";
        var message   = $"ayarliyo{salonPart}: {serviceName} ziyaretiniz tamamlandi! " +
                        $"Deneyiminizi degerlendirin: {reviewUrl}";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" Salon: {salonName}.";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var message     = $"ayarliyo: Randevunuz guncellendi!{salonPart} " +
                          $"Yeni tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm}, " +
                          $"Hizmet: {serviceName}, Personel: {staffName}.{bookingPart}";
        await SendSmsAsync(phone, message);
    }

    private async Task SendSmsAsync(string phone, string message)
    {
        await MessageResource.CreateAsync(
            body: message,
            from: new Twilio.Types.PhoneNumber(_fromNumber),
            to:   new Twilio.Types.PhoneNumber(FormatPhone(phone))
        );
    }

    private static string FormatPhone(string phone)
    {
        // Twilio E.164 formatı: +905551234567
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (phone.StartsWith("0"))
            phone = "+90" + phone[1..];
        else if (!phone.StartsWith("+"))
            phone = "+90" + phone;
        return phone;
    }

    private static DateTime ToTurkeyTime(DateTime utcTime)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        try   { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")); }
        catch { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")); }
    }
}
