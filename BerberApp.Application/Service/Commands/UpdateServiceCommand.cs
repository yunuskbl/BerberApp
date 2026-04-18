using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Service.DTOs;
using MediatR;

namespace BerberApp.Application.Service.Commands;

public class UpdateServiceCommand : IRequest<ServiceDto>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Color { get; set; }
    public bool IsActive { get; set; }
}