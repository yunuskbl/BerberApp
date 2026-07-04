using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Auth.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Auth.Handlers;

public class RegisterHandler : IRequestHandler<RegisterCommand, LoginResponse>
{
    private readonly IAppDbContext _context;
    private readonly IJwtTokenService _jwtService;
    private readonly IEmailService _emailService;
    private readonly IAppSettings _appSettings;

    public RegisterHandler(
        IAppDbContext context,
        IJwtTokenService jwtService,
        IEmailService emailService,
        IAppSettings appSettings)
    {
        _context = context;
        _jwtService = jwtService;
        _emailService = emailService;
        _appSettings = appSettings;
    }

    public async Task<LoginResponse> Handle(RegisterCommand request, CancellationToken ct)
    {
        // Telefon OTP doğrulaması kontrolü
        var verifiedOtp = await _context.OtpRecords
            .Where(x => x.Phone == request.Phone &&
                        x.IsVerified &&
                        x.VerifiedAt != null &&
                        x.VerifiedAt > DateTime.UtcNow.AddMinutes(-30))
            .FirstOrDefaultAsync(ct);

        if (verifiedOtp is null)
            throw new BadRequestException("Lütfen önce telefon numaranızı doğrulayın.");

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

        // Telefon kontrolü (zaten doğrulandıysa kayıtlı olmamalı)
        var phoneExists = await _context.Users
            .AnyAsync(x => x.Phone == request.Phone, ct);

        if (phoneExists)
            throw new ConflictException("Bu telefon numarasıyla zaten bir hesap mevcut.");

        // OTP kaydını kullanıldı olarak işaretle ve sil
        _context.OtpRecords.Remove(verifiedOtp);

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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
        };
        _context.Users.Add(user);

        // 30 günlük ücretsiz deneme aboneliği
        var trialEnd = DateTime.UtcNow.AddDays(30);
        var trial = new BerberApp.Domain.Entities.Subscription
        {
            TenantId   = tenant.Id,
            Plan       = PlanType.Baslangic,
            Status     = SubscriptionStatus.Trial,
            StartDate  = DateTime.UtcNow,
            ExpiryDate = trialEnd,
            Price      = 0,
            Currency   = "TRY",
        };
        _context.Subscriptions.Add(trial);
        await _context.SaveChangesAsync(ct);

        // Token üret
        var userPlan = PlanType.Baslangic;
        var accessToken = _jwtService.GenerateAccessToken(user, userPlan);
        var refreshToken = _jwtService.GenerateRefreshToken();

        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _context.SaveChangesAsync(ct);

        // Hoşgeldin emaili gönder (arka planda, başarısız olursa kayıt engelleme)
        var fullName = $"{request.FirstName} {request.LastName}";
        _ = _emailService.SendWelcomeEmailAsync(request.Email, fullName, request.TenantName);

        // Platform yöneticisine yeni kayıt bildirimi (arka planda)
        _ = _emailService.SendNewTenantRegisteredAsync(
            request.TenantName, request.Subdomain, fullName, request.Email, request.Phone ?? "");

        return new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            AccessTokenExpiry = DateTime.UtcNow.AddMinutes(15),
            Email = user.Email,
            FullName = fullName,
            Role = user.Role.ToString(),
            TenantId = user.TenantId,
            Subdomain = tenant.Subdomain,
            IsOnTrial = true,
            TrialEndsAt = trialEnd,
            SubscriptionExpired = false,
            IsEmailVerified = true,
        };
    }
}
