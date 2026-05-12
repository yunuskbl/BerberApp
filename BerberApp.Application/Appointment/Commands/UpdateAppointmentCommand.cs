using BerberApp.Application.Appointment.DTOs;
using MediatR;

namespace BerberApp.Application.Appointment.Commands;

public class UpdateAppointmentCommand : IRequest<AppointmentDto>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartTime { get; set; }
    public string? Notes { get; set; }
}
