using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Tenant.DTOs;
using MediatR;

namespace BerberApp.Application.Tenant.Commands;

public class UpdateTenantCommand : IRequest<TenantDto>
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? NotificationPhone { get; set; }
    public string? Address { get; set; }
    public string? ThemeColor { get; set; }
}