using MediatR;

namespace BerberApp.Application.Auth.Commands;

public class ForgotPasswordCommand : IRequest
{
    public string Phone { get; set; } = string.Empty;
}
