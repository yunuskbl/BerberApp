using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Application.Appointment.DTOs;
using MediatR;

namespace BerberApp.Application.Appointment.Queries;

public class GetAllAppointmentsQuery : IRequest<List<AppointmentDto>>
{
    public Guid TenantId { get; set; }
    public Guid? StaffId { get; set; }
    public DateTime? Date { get; set; }
}