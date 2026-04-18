using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Customer.DTOs;
using MediatR;

namespace BerberApp.Application.Customer.Queries;

public class GetCustomerByIdQuery : IRequest<CustomerDto>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}