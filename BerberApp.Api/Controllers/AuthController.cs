using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAuditLogService _audit;

    public AuthController(IMediator mediator, IAuditLogService audit)
    {
        _mediator = mediator;
        _audit    = audit;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var ip     = Request.Headers["X-Real-IP"].FirstOrDefault()
                  ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua     = Request.Headers.UserAgent.ToString();
        var path   = Request.Path.Value ?? "/api/auth/login";
        var method = Request.Method;

        try
        {
            var result = await _mediator.Send(command);
            await _audit.LogAsync("LOGIN_SUCCESS", "Info", ip, path, method,
                userAgent: ua, description: $"E-posta: {MaskEmail(command.Email)}");
            return Ok(new { success = true, message = "Giriş başarılı.", data = result });
        }
        catch
        {
            await _audit.LogAsync("LOGIN_FAILED", "Warning", ip, path, method,
                userAgent: ua, description: $"E-posta: {MaskEmail(command.Email)}");
            throw;
        }
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var ip     = Request.Headers["X-Real-IP"].FirstOrDefault()
                  ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var ua     = Request.Headers.UserAgent.ToString();
        var path   = Request.Path.Value ?? "/api/auth/register";
        var method = Request.Method;

        try
        {
            var result = await _mediator.Send(command);
            await _audit.LogAsync("TENANT_REGISTERED", "Info", ip, path, method,
                tenantId: result.TenantId.ToString(), userAgent: ua,
                description: $"Yeni işletme: {command.TenantName} ({command.Subdomain}) — {MaskEmail(command.Email)}");
            return Ok(new { success = true, message = "Kayıt başarılı.", data = result });
        }
        catch
        {
            await _audit.LogAsync("REGISTER_FAILED", "Warning", ip, path, method,
                userAgent: ua,
                description: $"Başarısız kayıt denemesi: {command.Subdomain} — {MaskEmail(command.Email)}");
            throw;
        }
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

        if (userIdClaim is null) return Unauthorized();

        command.UserId = Guid.Parse(userIdClaim.Value);
        var result = await _mediator.Send(command);
        return Ok(new { success = true, data = result, message = "Şifre değiştirildi." });
    }

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { success = true, message = "Eğer bu numaraya kayıtlı hesap varsa SMS gönderildi." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordCommand command)
    {
        await _mediator.Send(command);
        return Ok(new { success = true, message = "Şifreniz başarıyla güncellendi." });
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");

        if (userIdClaim is null) return Unauthorized();

        await _mediator.Send(new LogoutCommand { UserId = Guid.Parse(userIdClaim.Value) });
        return Ok(new { success = true, message = "Çıkış yapıldı." });
    }

    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<IActionResult> ResendVerification()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");

        if (userIdClaim is null) return Unauthorized();

        await _mediator.Send(new ResendEmailVerificationCommand { UserId = Guid.Parse(userIdClaim.Value) });
        return Ok(new { success = true, message = "Doğrulama e-postası gönderildi." });
    }

    [HttpGet("verify-email")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyEmail([FromQuery] string token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return BadRequest(new { success = false, message = "Geçersiz doğrulama bağlantısı." });

        var result = await _mediator.Send(new VerifyEmailCommand { Token = token });
        if (!result)
            return BadRequest(new { success = false, message = "Bağlantı geçersiz veya süresi dolmuş." });

        return Ok(new { success = true, message = "E-posta adresiniz başarıyla doğrulandı." });
    }

    [HttpDelete("delete-account")]
    [Authorize]
    public async Task<IActionResult> DeleteAccount([FromBody] DeleteAccountCommand command)
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)
                      ?? User.FindFirst("sub");

        if (userIdClaim is null) return Unauthorized();

        command.UserId = Guid.Parse(userIdClaim.Value);
        await _mediator.Send(command);
        return Ok(new { success = true, message = "Hesabınız ve kişisel verileriniz başarıyla silindi. (KVKK Madde 7)" });
    }

    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email)) return "***";
        var at = email.IndexOf('@');
        return at <= 1 ? "***" : $"{email[0]}***{email[at..]}";
    }
}
