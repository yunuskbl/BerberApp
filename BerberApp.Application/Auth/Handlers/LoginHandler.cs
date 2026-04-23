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

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public LoginHandler(IAppDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException("Email veya şifre hatalı.");

        // Tenant'ı al
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == user.TenantId, ct);

        var userPlan = PlanType.Basic;
        Subscription subscription = null;
        // Aktif aboneliği al
        try
        {
            subscription = await _context.Subscriptions
       .Where(x => x.TenantId == user.TenantId &&
                x.Status == SubscriptionStatus.Active &&
                x.ExpiryDate > DateTime.UtcNow)
       .OrderByDescending(x => x.StartDate)
       .FirstOrDefaultAsync(ct);

            userPlan = subscription?.Plan ?? PlanType.Basic;  // ← Default: Basic
        }
        catch (Exception)
        {

            userPlan = subscription?.Plan ?? PlanType.Basic;
        }
        

        var accessToken = _jwtService.GenerateAccessToken(user, userPlan);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(ct);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            Subdomain = tenant?.Subdomain
        };

    }
}