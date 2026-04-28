using BerberApp.Application.Auth.DTOs;
using MediatR;

namespace BerberApp.Application.Auth.Commands;

public class RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public string RefreshToken { get; set; } = string.Empty;
}
