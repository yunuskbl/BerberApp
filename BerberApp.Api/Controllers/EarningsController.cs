using BerberApp.Application.Appointment.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EarningsController : ControllerBase
{
    private readonly IMediator _mediator;

    public EarningsController(IMediator mediator) => _mediator = mediator;

    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? staffId,
        CancellationToken ct)
    {
        var tenantId = User.FindFirst("tenant_id");
        if (tenantId == null)
            return Unauthorized();

        var query = new GetEarningsQuery
        {
            TenantId = Guid.Parse(tenantId.Value),
            StartDate = startDate,
            EndDate = endDate,
            StaffId = staffId
        };

        var result = await _mediator.Send(query, ct);
        return Ok(new { success = true, data = result });
    }
}