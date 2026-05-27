using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class EarningsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<EarningsController> _logger;
    private readonly IAppDbContext _context;

    public EarningsController(IMediator mediator, ILogger<EarningsController> logger, IAppDbContext context)
    {
        _mediator = mediator;
        _logger = logger;
        _context = context;
    }

    /// <summary>
    /// Get earnings report for date range.
    /// Optional: branchId={guid} → specific branch; consolidated=true → all branches aggregated.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetEarnings(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? staffId,
        [FromQuery] Guid? branchId,
        [FromQuery] bool consolidated = false,
        CancellationToken ct = default)
    {
        try
        {
            var tenantIdClaim = User.FindFirst("tenant_id");
            if (tenantIdClaim == null)
                return Unauthorized();

            if (!Guid.TryParse(tenantIdClaim.Value, out var tenantId))
                return BadRequest(new { success = false, message = "Geçersiz salon ID" });

            if (startDate >= endDate)
                return BadRequest(new { success = false, message = "Başlangıç tarihi bitiş tarihinden önce olmalıdır" });

            if ((endDate - startDate).TotalDays > 365)
                return BadRequest(new { success = false, message = "Maksimum 1 yıllık rapor alınabilir" });

            List<Guid> tenantIds;

            if (consolidated)
            {
                var branchIds = await _context.Tenants
                    .Where(t => t.ParentTenantId == tenantId && !t.IsDeleted)
                    .Select(t => t.Id)
                    .ToListAsync(ct);
                tenantIds = new List<Guid> { tenantId };
                tenantIds.AddRange(branchIds);
            }
            else if (branchId.HasValue)
            {
                var isOwned = await _context.Tenants
                    .AnyAsync(t => t.Id == branchId.Value && t.ParentTenantId == tenantId && !t.IsDeleted, ct);
                if (!isOwned)
                    return StatusCode(403, new { success = false, message = "Bu şube size ait değil." });
                tenantIds = new List<Guid> { branchId.Value };
            }
            else
            {
                tenantIds = new List<Guid> { tenantId };
            }

            var query = new GetEarningsQuery
            {
                TenantId  = tenantId,
                TenantIds = tenantIds,
                StartDate = startDate,
                EndDate   = endDate,
                StaffId   = staffId
            };

            var result = await _mediator.Send(query, ct);
            return Ok(new { success = true, data = result });
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