using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Customer.DTOs;
using MediatR;

namespace BerberApp.Application.Customer.Queries;

public class GetAllCustomersQuery : IRequest<List<CustomerDto>>
{
    public Guid TenantId { get; set; }
}