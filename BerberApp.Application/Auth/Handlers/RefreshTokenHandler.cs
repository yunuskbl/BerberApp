using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Auth.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Auth.Handlers;

public class RefreshTokenHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _jwtService;

    public RefreshTokenHandler(IAppDbContext context, IJwtTokenService jwtService)
    {
        _context = context;
        _jwtService = jwtService;
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.RefreshToken == request.RefreshToken, ct);

        if (user is null
            || user.RefreshTokenExpiry is null
            || user.RefreshTokenExpiry < DateTime.UtcNow)
        {
            throw new UnauthorizedException("Geçersiz veya süresi dolmuş refresh token.");
        }

        var subscription = await _context.Subscriptions
            .Where(x => x.TenantId == user.TenantId
                     && x.Status == SubscriptionStatus.Active
                     && x.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(ct);

        var plan = subscription?.Plan ?? PlanType.Basic;

        var newAccessToken = _jwtService.GenerateAccessToken(user, plan);
        var newRefreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(ct);

        return new RefreshTokenResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken
        };
    }
}
