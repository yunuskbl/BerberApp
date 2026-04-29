using MediatR;

namespace BerberApp.Application.Auth.Commands;

public class ResetPasswordCommand : IRequest
{
    public string Phone { get; set; } = string.Empty;
    public string Otp { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}
