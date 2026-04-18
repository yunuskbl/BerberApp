using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace BerberApp.Application.Appointment.Commands;

public class CompleteAppointmentCommand : IRequest<bool>
{
    public Guid TenantId { get; set; }
    public Guid Id { get; set; }
}