using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Sms;
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
        // dil=TR: Netgsm'in Türkçe karakter destekli gönderimi (155 karakter/parça).
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
}
