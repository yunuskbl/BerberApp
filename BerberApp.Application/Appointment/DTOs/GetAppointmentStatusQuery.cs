using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Appointment.DTOs
{
    public class GetAppointmentStatusQuery : IRequest<AppointmentStatusDto>
    {
        public Guid AppointmentId { get; set; }
        public Guid TenantId { get; set; }
    }
}
