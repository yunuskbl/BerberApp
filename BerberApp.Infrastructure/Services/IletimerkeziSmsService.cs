using System.Text;
using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Infrastructure.Services;

public class IletimerkeziSmsService : ISmsService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _hash;
    private readonly string _sender;

    public IletimerkeziSmsService(IConfiguration config, HttpClient http)
    {
        _http   = http;
        _apiKey = config["IletiMerkezi:ApiKey"]!;
        _hash   = config["IletiMerkezi:Hash"]!;
        _sender = config["IletiMerkezi:Sender"] ?? "ayarliyo";
    }

    public Task SendOtpAsync(string phone, string otp)
    {
        var text = $"ayarlıyo dogrulama kodunuz: {otp}. Bu kod 5 dakika gecerlidir.";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" Salon: {salonName}.";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var text        = $"ayarliyo: Randevunuz onaylandi! " +
                          $"Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm}, " +
                          $"Hizmet: {serviceName}, Personel: {staffName}.{salonPart}{bookingPart}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var text        = $"ayarliyo: Yarin randevunuz var! " +
                          $"{turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} - {serviceName}.{bookingPart}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var salonPart   = string.IsNullOrWhiteSpace(salonName)  ? "" : $" ({salonName})";
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Yeni randevu: {bookingUrl}";
        var text        = $"ayarliyo{salonPart}: " +
                          $"{turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} " +
                          $"tarihli randevunuz iptal edildi.{bookingPart}";
        return SendAsync(phone, text);
    }

    public Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName,
        string salonName, string reviewUrl)
    {
        var salonPart = string.IsNullOrWhiteSpace(salonName) ? "" : $" ({salonName})";
        var text      = $"ayarliyo{salonPart}: {serviceName} ziyaretiniz tamamlandi! " +
                        $"Deneyiminizi degerlendirin: {reviewUrl}";
        return SendAsync(phone, text);
    }

    // -------------------------------------------------------------------------

    private async Task SendAsync(string phone, string text)
    {
        var formattedPhone = FormatPhone(phone);

        var payload = new
        {
            request = new
            {
                authentication = new { key = _apiKey, hash = _hash },
                order = new
                {
                    sender = _sender,
                    message = new
                    {
                        text,
                        receipents = new { number = new[] { formattedPhone } }
                    }
                }
            }
        };

        var json    = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync("https://api.iletimerkezi.com/v1/send-sms/json", content);
        var body     = await response.Content.ReadAsStringAsync();

        using var doc = JsonDocument.Parse(body);
        var status    = doc.RootElement
                           .GetProperty("response")
                           .GetProperty("status")
                           .GetProperty("code")
                           .GetInt32();

        if (status != 200)
        {
            var message = doc.RootElement
                             .GetProperty("response")
                             .GetProperty("status")
                             .GetProperty("message")
                             .GetString();
            throw new Exception($"IletiMerkezi hatası ({status}): {message}");
        }
    }

    private static string FormatPhone(string phone)
    {
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (phone.StartsWith("0"))
            return "90" + phone[1..];
        if (phone.StartsWith("+"))
            return phone[1..];
        if (phone.StartsWith("90"))
            return phone;
        return "90" + phone;
    }

    public Task SendAppointmentUpdatedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => Task.CompletedTask; // IletiMerkezi yapılandırılmadığında sessizce geç

    public Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
    {
        var turkeyTime  = ToTurkeyTime(startTime);
        var culture     = new System.Globalization.CultureInfo("tr-TR");
        var bookingPart = string.IsNullOrWhiteSpace(bookingUrl) ? "" : $" Randevu: {bookingUrl}";
        var text        = $"ayarliyo: Randevunuz 1 saat sonra! " +
                          $"{turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} - {serviceName}.{bookingPart}";
        return SendAsync(phone, text);
    }

    private static DateTime ToTurkeyTime(DateTime utcTime)
    {
        if (utcTime.Kind != DateTimeKind.Utc)
            utcTime = DateTime.SpecifyKind(utcTime, DateTimeKind.Utc);
        try   { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time")); }
        catch { return TimeZoneInfo.ConvertTimeFromUtc(utcTime, TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul")); }
    }
}
