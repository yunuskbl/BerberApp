using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Sms;
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

    public Task SendOtpAsync(string phone, string otp, string salonName = "")
        => SendSmsAsync(phone, SmsTemplates.Otp(otp, salonName));

    public Task SendAppointmentConfirmedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => SendSmsAsync(phone, SmsTemplates.AppointmentConfirmed(
            customerName, serviceName, staffName, startTime, salonName, bookingUrl));

    public Task SendAppointmentReminderAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => SendSmsAsync(phone, SmsTemplates.AppointmentReminder(
            customerName, serviceName, startTime, salonName, bookingUrl));

    public Task SendAppointmentReminder1hAsync(
        string phone, string customerName, string serviceName,
        DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "")
        => SendSmsAsync(phone, SmsTemplates.AppointmentReminder1h(
            customerName, serviceName, startTime, salonName));

    public Task SendAppointmentCancelledAsync(
        string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => SendSmsAsync(phone, SmsTemplates.AppointmentCancelled(
            customerName, startTime, salonName, bookingUrl));

    public Task SendAppointmentCompletedAsync(
        string phone, string customerName, string serviceName, string salonName, string reviewUrl)
        => SendSmsAsync(phone, SmsTemplates.AppointmentCompleted(
            customerName, serviceName, salonName, reviewUrl));

    public Task SendAppointmentUpdatedAsync(
        string phone, string customerName, string serviceName,
        string staffName, DateTime startTime, string salonName = "", string bookingUrl = "")
        => SendSmsAsync(phone, SmsTemplates.AppointmentUpdated(
            customerName, serviceName, staffName, startTime, salonName, bookingUrl));

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
}
