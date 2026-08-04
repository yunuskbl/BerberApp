using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Common.Interfaces
{
    public interface ISmsService
    {
        /// <param name="salonName">
        /// Kodun hangi işletme için istendiği. Boş bırakılırsa mesaj yalnızca ayarlıyo
        /// adına gönderilir (işletme kaydı, şifre sıfırlama gibi salon bağlamı olmayan akışlar).
        /// </param>
        Task SendOtpAsync(string phone, string otp, string salonName = "");
        Task SendAppointmentConfirmedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "");
        Task SendAppointmentReminderAsync(string phone, string customerName, string serviceName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "");
        Task SendAppointmentReminder1hAsync(string phone, string customerName, string serviceName, DateTime startTime, string salonName = "", string mapsUrl = "", string bookingUrl = "");
        Task SendAppointmentCancelledAsync(string phone, string customerName, DateTime startTime, string salonName = "", string bookingUrl = "");
        Task SendAppointmentCompletedAsync(string phone, string customerName, string serviceName, string salonName, string reviewUrl);
        Task SendAppointmentUpdatedAsync(string phone, string customerName, string serviceName, string staffName, DateTime startTime, string salonName = "", string bookingUrl = "");
    }
}
