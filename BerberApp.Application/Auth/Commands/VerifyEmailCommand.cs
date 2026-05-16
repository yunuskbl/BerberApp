using MediatR;

namespace BerberApp.Application.Auth.Commands;

public class VerifyEmailCommand : IRequest<bool>
{
    public string Token { get; set; } = string.Empty;
}
