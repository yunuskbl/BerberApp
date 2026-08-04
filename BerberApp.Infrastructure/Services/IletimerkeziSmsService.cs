using System.Text;
using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Sms;
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
        => SendAsync(phone, SmsTemplates.Otp(otp));

    public Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => SendAsync(phone, SmsTemplates.AppointmentConfirmed(
            customerName, serviceName, staffName, startTime, salonName, bookingUrl));

    public Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => SendAsync(phone, SmsTemplates.AppointmentReminder(
            customerName, serviceName, startTime, salonName, bookingUrl));

    public Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => SendAsync(phone, SmsTemplates.AppointmentReminder1h(
            customerName, serviceName, startTime, salonName));

    public Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => SendAsync(phone, SmsTemplates.AppointmentCancelled(
            customerName, startTime, salonName, bookingUrl));

    public Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName, string salonName, string reviewUrl)
        => SendAsync(phone, SmsTemplates.AppointmentCompleted(
            customerName, serviceName, salonName, reviewUrl));

    public Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => SendAsync(phone, SmsTemplates.AppointmentUpdated(
            customerName, serviceName, staffName, startTime, salonName, bookingUrl));

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
}
