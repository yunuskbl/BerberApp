using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Tenant.DTOs;
using MediatR;

namespace BerberApp.Application.Tenant.Queries;

public class GetTenantByIdQuery : IRequest<TenantDto>
{
    public Guid Id { get; set; }
}
