using MediatR;

namespace BerberApp.Application.Appointment.Commands;

public class CancelAppointmentCommand : IRequest<bool>
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
}