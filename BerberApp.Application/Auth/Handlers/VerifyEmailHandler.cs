using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Auth.Handlers;

public class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, bool>
{
    private readonly IAppDbContext _context;

    public VerifyEmailHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(VerifyEmailCommand request, CancellationToken ct)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u =>
                u.EmailVerificationToken == request.Token &&
                u.EmailVerificationTokenExpiry > DateTime.UtcNow, ct);

        if (user is null) return false;

        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        await _context.SaveChangesAsync(ct);

        return true;
    }
}
