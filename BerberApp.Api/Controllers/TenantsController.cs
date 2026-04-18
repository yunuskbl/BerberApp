using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BerberApp.Application.Tenant.Commands;
using BerberApp.Application.Tenant.Queries;
using MediatR;

namespace BerberApp.Api.Controllers;

public class TenantsController : BaseApiController
{
    public TenantsController(IMediator mediator) : base(mediator) { }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyTenant()
        => Success(await Mediator.Send(new GetTenantByIdQuery { Id = TenantId }));

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Success(await Mediator.Send(new GetAllTenantsQuery()));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTenantCommand command)
    {
        command.Id = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteTenantCommand { Id = id });
        return NoContent();
    }
}
