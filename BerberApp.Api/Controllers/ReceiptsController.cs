using BerberApp.Application.Receipt.Commands;
using BerberApp.Application.Receipt.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class ReceiptsController : BaseApiController
{
    public ReceiptsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] Guid? customerId)
        => Success(await Mediator.Send(new GetReceiptsByTenantQuery
        {
            TenantId   = TenantId,
            From       = from,
            To         = to,
            CustomerId = customerId,
        }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await Mediator.Send(new GetReceiptByIdQuery { Id = id, TenantId = TenantId });
        if (result is null) return NotFound(new { success = false, message = "Fiş bulunamadı." });
        return Success(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateReceiptCommand command)
    {
        command.TenantId = TenantId;
        return Created(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Void(Guid id)
    {
        var ok = await Mediator.Send(new VoidReceiptCommand { Id = id, TenantId = TenantId });
        if (!ok) return NotFound(new { success = false, message = "Fiş bulunamadı." });
        return Success<object>(null!, "Fiş iptal edildi.");
    }
}
