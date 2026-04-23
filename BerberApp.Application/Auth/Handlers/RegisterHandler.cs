using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Auth.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.Auth.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, LoginResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public RegisterHandler(IAppDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        // Subdomain kontrolü
        var subdomainExists = await _context.Tenants
            .AnyAsync(x => x.Subdomain == request.Subdomain, ct);

        if (subdomainExists)
            throw new ConflictException($"'{request.Subdomain}' subdomain'i zaten kullanılıyor.");

        // Email kontrolü
        var emailExists = await _context.Users
            .AnyAsync(x => x.Email == request.Email, ct);

        if (emailExists)
            throw new ConflictException($"'{request.Email}' email adresi zaten kayıtlı.");

        // Tenant oluştur
        var tenant = new TenantEntity
        {
            Name = request.TenantName,
            Subdomain = request.Subdomain,
            Phone = request.Phone,
            Address = request.Address,
            IsActive = true
        };

        _context.Tenants.Add(tenant);

        // Admin kullanıcı oluştur
        var user = new User
        {
            TenantId = tenant.Id,
            Email = request.Email,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Phone = request.Phone,
            Role = UserRole.Admin,
            IsVerified = true,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password)
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(ct);

        // Yeni user'a default plan ver (Basic)
        var userPlan = BerberApp.Domain.Enums.PlanType.Basic;

        // Token üret
        var accessToken = _jwtService.GenerateAccessToken(user, userPlan);
        var refreshToken = _jwtService.GenerateRefreshToken();

        // Refresh token kaydet
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(ct);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = user.Role.ToString(),
            TenantId = user.TenantId
        };
    }
}