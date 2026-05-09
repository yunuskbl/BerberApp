using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Common.Interfaces
{
    public interface ISmsService
    {
        Task SendOtpAsync(string phone, string otp);
        Task SendAppointmentConfirmedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime, string salonName = "", string address = "");
        Task SendAppointmentReminderAsync(string phone, string customerName, string serviceName, DateTime startTime, string salonName = "", string address = "");
        Task SendAppointmentCancelledAsync(string phone, string customerName, DateTime startTime, string salonName = "");
        Task SendAppointmentCompletedAsync(string phone, string customerName, string serviceName, string salonName, string reviewUrl);
    }
}
