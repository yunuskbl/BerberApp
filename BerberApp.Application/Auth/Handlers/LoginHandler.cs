using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Auth.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BerberApp.Application.Auth.Handlers;

public class LoginHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _jwtService;
    private readonly ILogger<LoginHandler> _logger;

    public LoginHandler(IAppDbContext context, IJwtTokenService jwtService, ILogger<LoginHandler> logger)
    {
        _context = context;
        _jwtService = jwtService;
        _logger = logger;
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(x => x.Email == request.Email, ct);

        if (user is null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
        {
            // Güvenlik logu: e-posta adresini maskele (@ öncesi gizle)
            _logger.LogWarning("[SECURITY:LOGIN_FAILED] Email={MaskedEmail}",
                MaskEmail(request.Email));
            throw new UnauthorizedException("Email veya şifre hatalı.");
        }

        // Tenant'ı al
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == user.TenantId, ct);

        var userPlan = PlanType.Baslangic;
        Subscription? subscription = null;
        // Aktif veya trial aboneliği al
        try
        {
            subscription = await _context.Subscriptions
                .Where(x => x.TenantId == user.TenantId &&
                       (x.Status == SubscriptionStatus.Active || x.Status == SubscriptionStatus.Trial) &&
                       x.ExpiryDate > DateTime.UtcNow)
                .OrderByDescending(x => x.StartDate)
                .FirstOrDefaultAsync(ct);

            // Abonelik yoksa (eski kayıt) otomatik 14 günlük trial oluştur
            if (subscription == null)
            {
                var trialEnd = DateTime.UtcNow.AddDays(14);
                subscription = new Subscription
                {
                    TenantId   = user.TenantId,
                    Plan       = PlanType.Baslangic,
                    Status     = SubscriptionStatus.Trial,
                    StartDate  = DateTime.UtcNow,
                    ExpiryDate = trialEnd,
                    Price      = 0,
                    Currency   = "TRY",
                };
                _context.Subscriptions.Add(subscription);
                await _context.SaveChangesAsync(ct);
            }

            userPlan = subscription.Plan;
        }
        catch (Exception)
        {
            userPlan = PlanType.Baslangic;
        }
        

        var accessToken = _jwtService.GenerateAccessToken(user, userPlan);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("[SECURITY:LOGIN_SUCCESS] UserId={UserId} TenantId={TenantId}",
            user.Id, user.TenantId);

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            Email = user.Email,
            FullName = $"{user.FirstName} {user.LastName}",
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            Subdomain = tenant?.Subdomain,
            IsOnTrial = subscription?.Status == SubscriptionStatus.Trial,
            TrialEndsAt = subscription?.Status == SubscriptionStatus.Trial ? subscription.ExpiryDate : null,
            SubscriptionExpired = subscription == null || subscription.ExpiryDate <= DateTime.UtcNow,
        };
    }

    /// <summary>E-posta adresini loglamak için maskeler: a***@domain.com</summary>
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrEmpty(email)) return "unknown";
        var at = email.IndexOf('@');
        if (at <= 1) return "***@***";
        return email[0] + "***" + (at < email.Length ? email[at..] : "");
    }
}