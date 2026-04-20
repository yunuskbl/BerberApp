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
        Task SendAppointmentConfirmedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime);
        Task SendAppointmentReminderAsync(string phone, string customerName, string serviceName, DateTime startTime);
        Task SendAppointmentCancelledAsync(string phone, string customerName, DateTime startTime);
    }
}
