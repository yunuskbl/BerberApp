using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Models;
using BerberApp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { success = true, message = "Giriş başarılı.", data = result });
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { success = true, message = "Kayıt başarılı.", data = result });
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await _mediator.Send(command);
        return Ok(new { success = true, data = result });
    }
    [HttpPost("change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordCommand command)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");

        if (userIdClaim is null)
            return Unauthorized();

        command.UserId = Guid.Parse(userIdClaim.Value);
        var result = await _mediator.Send(command);
        return Ok(new { success = true, data = result, message = "Şifre değiştirildi." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");

        if (userIdClaim is null)
            return Unauthorized();

        await _mediator.Send(new LogoutCommand { UserId = Guid.Parse(userIdClaim.Value) });
        return Ok(new { success = true, message = "Çıkış yapıldı." });
    }

}