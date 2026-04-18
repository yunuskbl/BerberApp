using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Customer.DTOs;
using MediatR;

namespace BerberApp.Application.Customer.Commands;

public class CreateCustomerCommand : IRequest<CustomerDto>
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Notes { get; set; }
}