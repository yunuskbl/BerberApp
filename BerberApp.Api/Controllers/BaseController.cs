using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    protected readonly IMediator Mediator;

    protected BaseApiController(IMediator mediator)
    {
        Mediator = mediator;
    }

    // JWT'den TenantId'yi otomatik al
    protected Guid TenantId => Guid.Parse(
        User.Claims.First(x => x.Type == "tenant_id").Value);

    // JWT'den UserId'yi otomatik al
    protected Guid UserId => Guid.Parse(
        User.Claims.First(x => x.Type == "sub").Value);

    // Başarılı response
    protected IActionResult Success<T>(T data, string message = "İşlem başarılı.")
        => Ok(new { success = true, message, data });

    // Oluşturuldu response
    protected IActionResult Created<T>(T data, string message = "Kayıt oluşturuldu.")
        => StatusCode(201, new { success = true, message, data });
}