using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MediatR;

namespace BerberApp.Application.Service.Commands;

public class DeleteServiceCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
