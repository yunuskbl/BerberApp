using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.WorkingHour.DTOs;
using MediatR;

namespace BerberApp.Application.WorkingHour.Commands;

public class CreateWorkingHourCommand : IRequest<WorkingHourDto>
{
    public Guid StaffId { get; set; }
    public Guid TenantId { get; set; }
    public DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public bool IsOff { get; set; }
}
