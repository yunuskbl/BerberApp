using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Auth.Handlers;

public class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand>
{
    private readonly IAppDbContext _context;
    private readonly ISmsService _smsService;

    public ForgotPasswordHandler(IAppDbContext context, ISmsService smsService)
    {
        _context = context;
        _smsService = smsService;
    }

    public async Task Handle(ForgotPasswordCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == request.Phone, ct);

        // Kullanıcı bulunamasa bile başarılı döndür (enumeration saldırısını önler)
        if (user == null) return;

        var otp = GenerateOtp();
        user.PasswordResetOtp = BCrypt.Net.BCrypt.HashPassword(otp);
        user.PasswordResetOtpExpiry = DateTime.UtcNow.AddMinutes(5);

        await _context.SaveChangesAsync(ct);
        await _smsService.SendOtpAsync(request.Phone, otp);
    }

    private static string GenerateOtp()
    {
        return Random.Shared.Next(100000, 999999).ToString();
    }
}
