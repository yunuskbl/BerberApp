using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Auth.Handlers;

public class ResendEmailVerificationHandler : IRequestHandler<ResendEmailVerificationCommand>
{
    private readonly IAppDbContext _context;
    private readonly IEmailService _emailService;
    private readonly IAppSettings _appSettings;

    public ResendEmailVerificationHandler(
        IAppDbContext context,
        IEmailService emailService,
        IAppSettings appSettings)
    {
        _context = context;
        _emailService = emailService;
        _appSettings = appSettings;
    }

    public async Task Handle(ResendEmailVerificationCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, ct);

        if (user is null || user.EmailVerificationToken == null) return;

        // Yeni token oluştur
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        await _context.SaveChangesAsync(ct);

        var verificationUrl = $"{_appSettings.FrontendBaseUrl}/email-dogrula?token={token}";
        var fullName = $"{user.FirstName} {user.LastName}";
        _ = _emailService.SendEmailVerificationAsync(user.Email, fullName, verificationUrl);
    }
}
