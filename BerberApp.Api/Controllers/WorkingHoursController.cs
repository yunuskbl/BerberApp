using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BerberApp.Application.WorkingHour.Commands;
using BerberApp.Application.WorkingHour.Queries;
using MediatR;

namespace BerberApp.Api.Controllers;

public class WorkingHoursController : BaseApiController
{
    public WorkingHoursController(IMediator mediator) : base(mediator) { }

    [HttpGet("staff/{staffId}")]
    public async Task<IActionResult> GetByStaff(Guid staffId)
        => Success(await Mediator.Send(new GetWorkingHoursByStaffQuery
        {
            StaffId = staffId,
            TenantId = TenantId
        }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateWorkingHourCommand command)
    {
        command.TenantId = TenantId;
        return Created(await Mediator.Send(command));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateWorkingHourCommand command)
    {
        command.Id = id;
        command.TenantId = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteWorkingHourCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }
}