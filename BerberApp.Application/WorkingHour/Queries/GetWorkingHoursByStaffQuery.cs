using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.WorkingHour.DTOs;
using MediatR;

namespace BerberApp.Application.WorkingHour.Queries;

public class GetWorkingHoursByStaffQuery : IRequest<List<WorkingHourDto>>
{
    public Guid StaffId { get; set; }
    public Guid TenantId { get; set; }
}