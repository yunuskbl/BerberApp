using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Auth.Handlers;

public class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand>
{
    private readonly IAppDbContext _context;

    public ResetPasswordHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ResetPasswordCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Phone == request.Phone, ct)
            ?? throw new BadRequestException("Geçersiz kod veya telefon numarası.");

        if (user.PasswordResetOtp == null || user.PasswordResetOtpExpiry == null)
            throw new BadRequestException("Geçersiz kod veya telefon numarası.");

        if (user.PasswordResetOtpExpiry < DateTime.UtcNow)
            throw new BadRequestException("Kod süresi dolmuş. Yeni kod talep edin.");

        if (!BCrypt.Net.BCrypt.Verify(request.Otp, user.PasswordResetOtp))
            throw new BadRequestException("Geçersiz kod veya telefon numarası.");

        if (request.NewPassword.Length < 6)
            throw new BadRequestException("Şifre en az 6 karakter olmalıdır.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetOtp = null;
        user.PasswordResetOtpExpiry = null;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;

        await _context.SaveChangesAsync(ct);
    }
}
