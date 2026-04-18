using BerberApp.Application.Customer.Commands;
using BerberApp.Application.Customer.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

public class CustomersController : BaseApiController
{
    public CustomersController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Success(await Mediator.Send(new GetAllCustomersQuery { TenantId = TenantId }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Success(await Mediator.Send(new GetCustomerByIdQuery { Id = id, TenantId = TenantId }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        command.TenantId = TenantId;
        return Created(await Mediator.Send(command));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerCommand command)
    {
        command.Id = id;
        command.TenantId = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCustomerCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }
}