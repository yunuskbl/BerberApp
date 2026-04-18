using BerberApp.Application.Service.Commands;
using BerberApp.Application.Service.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

public class ServicesController : BaseApiController
{
    public ServicesController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Success(await Mediator.Send(new GetAllServicesQuery { TenantId = TenantId }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Success(await Mediator.Send(new GetServiceByIdQuery { Id = id, TenantId = TenantId }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateServiceCommand command)
    {
        command.TenantId = TenantId;
        return Created(await Mediator.Send(command));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceCommand command)
    {
        command.Id = id;
        command.TenantId = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteServiceCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }
}