using BerberApp.Application.Appointment.Queries;
using BerberApp.Domain.Enums;
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
    private readonly ILogger<EarningsController> _logger;

    public EarningsController(IMediator mediator, ILogger<EarningsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>
    /// Get earnings report for date range
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEarnings(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? staffId,
        CancellationToken ct)
    {
        try
        {
            // Get tenant ID from claims
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim == null)
                return Unauthorized();

            if (!Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                return BadRequest(new { success = false, message = "Geçersiz salon ID" });

            // Validate dates
            if (startDate >= endDate)
                return BadRequest(new { success = false, message = "Başlangıç tarihi bitiş tarihinden önce olmalıdır" });

            if ((endDate - startDate).TotalDays > 365)
                return BadRequest(new { success = false, message = "Maksimum 1 yıllık rapor alınabilir" });

            var query = new GetEarningsQuery
            {
                TenantId = tenantId,
                StartDate = startDate,
                EndDate = endDate,
                StaffId = staffId
            };

            var result = await _mediator.Send(query, ct);

            return Ok(new
            {
                success = true,
                data = result
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Earnings report error");
            return StatusCode(500, new { success = false, message = "Rapor alınırken hata oluştu" });
        }
    }

    /// <summary>
    /// Get earnings breakdown by service
    /// </summary>

    /// <summary>
    /// Get earnings breakdown by staff
    /// </summary>
}