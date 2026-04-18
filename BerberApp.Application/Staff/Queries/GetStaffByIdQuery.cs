using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Staff.DTOs;
using MediatR;

namespace BerberApp.Application.Staff.Queries;

public class GetStaffByIdQuery : IRequest<StaffDto>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
