using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace BerberApp.Application.Tenant.Commands;

public class DeleteTenantCommand : IRequest<bool>
{
    public Guid Id { get; set; }
}
