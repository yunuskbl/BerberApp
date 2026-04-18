using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.Queries;
using MediatR;

namespace BerberApp.Api.Controllers;

public class AppointmentsController : BaseApiController
{
    public AppointmentsController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? staffId, [FromQuery] DateTime? date)
        => Success(await Mediator.Send(new GetAllAppointmentsQuery
        {
            TenantId = TenantId,
            StaffId = staffId,
            Date = date
        }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Success(await Mediator.Send(new GetAppointmentByIdQuery
        {
            Id = id,
            TenantId = TenantId
        }));

    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] Guid staffId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime date)
        => Success(await Mediator.Send(new GetAvailableSlotsQuery
        {
            TenantId = TenantId,
            StaffId = staffId,
            ServiceId = serviceId,
            Date = date
        }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentCommand command)
    {
        command.TenantId = TenantId;
        return Created(await Mediator.Send(command));
    }

    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await Mediator.Send(new CancelAppointmentCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        await Mediator.Send(new CompleteAppointmentCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }
}
