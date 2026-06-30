using System.Globalization;
using System.Net;
using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BerberApp.Infrastructure.Services;

public class NetgsmSmsService : ISmsService
{
    private readonly HttpClient _http;
    private readonly string _userCode;
    private readonly string _password;
    private readonly string _msgHeader;
    private readonly ILogger<NetgsmSmsService> _log;

    public NetgsmSmsService(IConfiguration config, HttpClient http, ILogger<NetgsmSmsService> log)
    {
        _http      = http;
        _userCode  = config["Netgsm:UserCode"]!;
        _password  = config["Netgsm:Password"]!;
        _msgHeader = config["Netgsm:MsgHeader"] ?? "ayarliyo";
        _log       = log;
    }

    public Task SendOtpAsync(string phone, string otp)
    {
        var text = $"ayarliyo dogrulama kodunuz: {otp}. Bu kod 5 dakika gecerlidir.";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var culture     = new CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" Salon: {salonName}.";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var text        = $"ayarliyo: Randevunuz onaylandi! " +
                          $"Tarih: {t.ToString("dd MMMM yyyy", culture)} {t:HH:mm}, " +
                          $"Hizmet: {serviceName}, Personel: {staffName}.{salonPart}{bookingPart}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var culture     = new CultureInfo("tr-TR");
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var text        = $"ayarliyo: Yarin randevunuz var! " +
                          $"{t.ToString("dd MMMM yyyy", culture)} {t:HH:mm} - {serviceName}.{bookingPart}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var culture     = new CultureInfo("tr-TR");
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var text        = $"ayarliyo: Randevunuz 1 saat sonra! " +
                          $"{t.ToString("dd MMMM yyyy", culture)} {t:HH:mm} - {serviceName}.{bookingPart}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var culture     = new CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" ({salonName})";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Yeni randevu: {bookingUrl}";
        var text        = $"ayarliyo{salonPart}: " +
                          $"{t.ToString("dd MMMM yyyy", culture)} {t:HH:mm} " +
                          $"tarihli randevunuz iptal edildi.{bookingPart}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName, string salonName, string reviewUrl)
    {
        var salonPart = string.IsNullOrWhiteSpace(salonName) ? "" : $" ({salonName})";
        var text      = $"ayarliyo{salonPart}: {serviceName} ziyaretiniz tamamlandi! " +
                        $"Deneyiminizi degerlendirin: {reviewUrl}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
    {
        var t           = ToTurkeyTime(startTime);
        var culture     = new CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" Salon: {salonName}.";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var text        = $"ayarliyo: Randevunuz guncellendi! " +
                          $"Yeni tarih: {t.ToString("dd MMMM yyyy", culture)} {t:HH:mm}, " +
                          $"Hizmet: {serviceName}, Personel: {staffName}.{salonPart}{bookingPart}";
        return SendAsync(phone, text);
    }

    // -------------------------------------------------------------------------

    private async Task SendAsync(string phone, string text)
    {
        var formattedPhone = FormatPhone(phone);
        var url = $"https://api.netgsm.com.tr/sms/send/get/" +
                  $"?usercode={Uri.EscapeDataString(_userCode)}" +
                  $"&password={Uri.EscapeDataString(_password)}" +
                  $"&gsmno={formattedPhone}" +
                  $"&message={Uri.EscapeDataString(text)}" +
                  $"&msgheader={Uri.EscapeDataString(_msgHeader)}" +
                  $"&dil=TR";

        _log.LogInformation("[Netgsm] SMS gönderiliyor → {Phone}", formattedPhone);

        var response = await _http.GetAsync(url);
        var body     = (await response.Content.ReadAsStringAsync()).Trim();

        _log.LogInformation("[Netgsm] Yanıt: {Body}", body);

        // Netgsm başarı yanıtı "00 {jobId}" formatındadır
        if (!body.StartsWith("00"))
            throw new InvalidOperationException($"Netgsm SMS hatası: {body}");
    }

    private static string FormatPhone(string phone)
    {
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (phone.StartsWith("0"))   return "90" + phone[1..];
        if (phone.StartsWith("+"))   return phone[1..];
        if (phone.StartsWith("90"))  return phone;
        return "90" + phone;
    }

    private static DateTime ToTurkeyTime(DateTime utcTime)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        try   { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")); }
        catch { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")); }
    }
}
