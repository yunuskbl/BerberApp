using BerberApp.Application.Appointment.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Common.Interfaces
{
    public interface INotificationService
    {
        Task SendAppointmentReceivedAsync(string recipient, AppointmentStatusDto dto);
        Task SendAppointmentConfirmedAsync(string recipient, AppointmentStatusDto dto);
        Task SendAppointmentCancelledAsync(string recipient, AppointmentStatusDto dto);
        Task SendAppointmentCompletedAsync(string recipient, AppointmentStatusDto dto, string reviewUrl);
        Task SendAppointmentUpdatedAsync(string recipient, AppointmentStatusDto dto);
    }
}
