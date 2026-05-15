using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Common.Interfaces;

public interface IWhatsAppService
{
    Task SendAppointmentConfirmedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "");
    Task SendAppointmentReminderAsync(string phone, string customerName, string serviceName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "");
    Task SendAppointmentCancelledAsync(string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "");
    Task SendOtpAsync(string phone, string otp);
    Task SendNewAppointmentRequestAsync(string staffPhone, string customerName, string customerPhone, string serviceName
        , DateTime startTime, int sequenceNumber);
    Task SendAppointmentCompletedAsync(string phone, string customerName, string serviceName, string salonName, string reviewUrl);
    Task SendAppointmentUpdatedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime, string salonName = "", string bookingUrl = "");
    Task SendMonthlyLimitWarningAsync(string phone, string salonName, int currentCount, int limit, bool isFull);
}
