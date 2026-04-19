using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Application.Appointment.DTOs;
using MediatR;

namespace BerberApp.Application.Appointment.Commands;

public class CreateAppointmentCommand : IRequest<AppointmentDto>
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartTime { get; set; }
    public string? Notes { get; set; }
    public bool IsFromBookingPage { get; set; } = false;
}
