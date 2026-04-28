using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Auth.Handlers;

public class LogoutHandler : IRequestHandler<LogoutCommand>
{
    private readonly IAppDbContext _context;

    public LogoutHandler(IAppDbContext context)
    {
        _context = context;
    }

    public async Task Handle(LogoutCommand request, CancellationToken ct)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.Id == request.UserId, ct);
        if (user is null) throw new UnauthorizedException("Kullanıcı bulunamadı.");

        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await _context.SaveChangesAsync(ct);
    }
}
