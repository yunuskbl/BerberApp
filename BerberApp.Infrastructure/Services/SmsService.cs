using BerberApp.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;

namespace BerberApp.Infrastructure.Services;

public class SmsService : ISmsService
{
    private readonly string _apiKey;
    private readonly string _hash;
    private readonly string _sender;
    private readonly HttpClient _httpClient;

    public SmsService(IConfiguration config, HttpClient httpClient)
    {
        _apiKey = config["IletiMerkezi:ApiKey"]!;
        _hash = config["IletiMerkezi:Hash"]!;
        _sender = config["IletiMerkezi:Sender"]!;
        _httpClient = httpClient;
    }

    public async Task SendOtpAsync(string phone, string otp)
    {
        var message = $"BerberApp dogrulama kodunuz: {otp}. Bu kod 5 dakika gecerlidir.";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentConfirmedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime)
    {
        var turkeyTime = ToTurkeyTime(startTime);
        var culture = new System.Globalization.CultureInfo("tr-TR");
        var message = $"BerberApp: Randevunuz onaylandi! Tarih: {turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm}, Hizmet: {serviceName}, Personel: {staffName}";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentReminderAsync(string phone, string customerName, string serviceName, DateTime startTime)
    {
        var turkeyTime = ToTurkeyTime(startTime);
        var culture = new System.Globalization.CultureInfo("tr-TR");
        var message = $"BerberApp: Yarin randevunuz var! {turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} - {serviceName}";
        await SendSmsAsync(phone, message);
    }

    public async Task SendAppointmentCancelledAsync(string phone, string customerName, DateTime startTime)
    {
        var turkeyTime = ToTurkeyTime(startTime);
        var culture = new System.Globalization.CultureInfo("tr-TR");
        var message = $"BerberApp: {turkeyTime.ToString("dd MMMM yyyy", culture)} {turkeyTime:HH:mm} tarihli randevunuz iptal edildi.";
        await SendSmsAsync(phone, message);
    }

    private async Task SendSmsAsync(string phone, string message)
    {
        phone = FormatPhone(phone);

        var payload = new
        {
            request = new
            {
                authentication = new
                {
                    key = _apiKey,
                    hash = _hash
                },
                order = new
                {
                    sender = _sender,
                    iys = "1",
                    iysList = "BIREYSEL",
                    message = new
                    {
                        text = message,
                        receipents = new
                        {
                            number = new[] { phone }
                        }
                    }
                }
            }
        };

        var json = JsonSerializer.Serialize(payload);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://api.iletimerkezi.com/v1/send-sms/json", content);
        response.EnsureSuccessStatusCode();
    }

    private static string FormatPhone(string phone)
    {
        phone = phone.Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "");
        if (phone.StartsWith("0"))
            phone = "90" + phone[1..];
        else if (phone.StartsWith("+"))
            phone = phone[1..];
        else if (!phone.StartsWith("90"))
            phone = "90" + phone;
        return phone;
    }

    private static DateTime ToTurkeyTime(DateTime utcTime)
    {
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