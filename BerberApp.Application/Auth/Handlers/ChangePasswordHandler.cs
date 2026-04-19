using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using MediatR;

namespace BerberApp.Application.Auth.Handlers;

public class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, bool>
{
    private readonly IGenericRepository<BerberApp.Domain.Entities.User> _userRepo;

    public ChangePasswordHandler(IGenericRepository<BerberApp.Domain.Entities.User> userRepo)
    {
        _userRepo = userRepo;
    }

    public async Task<bool> Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var user = await _userRepo.GetByIdAsync(request.UserId, ct);
        if (user is null)
            throw new NotFoundException("Kullanıcı", request.UserId);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new BadRequestException("Mevcut şifre hatalı.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _userRepo.UpdateAsync(user, ct);

        return true;
    }
}