using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BerberApp.Application.Auth.DTOs;
using MediatR;

namespace BerberApp.Application.Auth.Commands;

public class RegisterCommand : IRequest<LoginResponse>
{
    public string TenantName { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
}