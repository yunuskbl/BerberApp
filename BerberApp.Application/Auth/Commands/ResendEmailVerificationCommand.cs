using MediatR;

namespace BerberApp.Application.Auth.Commands;

public class ResendEmailVerificationCommand : IRequest
{
    public Guid UserId { get; set; }
}
