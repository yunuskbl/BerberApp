using MediatR;

namespace BerberApp.Application.Auth.Commands;

public class LogoutCommand : IRequest
{
    public Guid UserId { get; set; }
}
