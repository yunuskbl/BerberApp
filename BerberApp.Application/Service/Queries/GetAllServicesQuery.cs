using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Service.DTOs;
using MediatR;

namespace BerberApp.Application.Service.Queries;

public class GetAllServicesQuery : IRequest<List<ServiceDto>>
{
    public Guid TenantId { get; set; }
}
