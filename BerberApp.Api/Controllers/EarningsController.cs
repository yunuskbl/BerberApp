using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.Queries;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin,SuperAdmin")]
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

            var (tenantIds, scopeError) = await ResolveScopeAsync(tenantId, branchId, consolidated, ct);
            if (scopeError != null) return scopeError;

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
    /// Müşteri takibi raporu: dönem içi KPI'lar, yeni/tekrar gelen zaman serisi,
    /// sadakat dağılımı, kayıp riskindeki müşteriler ve en çok harcayanlar.
    /// Kapsam parametreleri gelir raporuyla aynı davranır.
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> GetCustomerInsights(
        [FromQuery] DateTime startDate,
        [FromQuery] DateTime endDate,
        [FromQuery] Guid? branchId,
        [FromQuery] bool consolidated = false,
        [FromQuery] int atRiskAfterDays = 60,
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

            var (tenantIds, scopeError) = await ResolveScopeAsync(tenantId, branchId, consolidated, ct);
            if (scopeError != null) return scopeError;

            var result = await _mediator.Send(new GetCustomerInsightsQuery
            {
                TenantId        = tenantId,
                TenantIds       = tenantIds,
                StartDate       = startDate,
                EndDate         = endDate,
                // Uç değerler anlamsız: 7 günden kısa bir eşik hâlâ gelmekte
                // olan müşteriyi "kayıp" sayar, 365 günü aşan eşik hiçbir şey
                // göstermez.
                AtRiskAfterDays = Math.Clamp(atRiskAfterDays, 7, 365),
            }, ct);

            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Customer insights report error");
            return StatusCode(500, new { success = false, message = "Rapor alınırken hata oluştu" });
        }
    }

    /// <summary>
    /// Raporun hangi işletmeleri kapsadığını çözer: ana işletme, tek bir şube
    /// ya da hepsi. Şube isteniyorsa gerçekten bu hesaba ait mi diye bakılır —
    /// aksi hâlde başka bir salonun cirosu okunabilirdi.
    /// </summary>
    private async Task<(List<Guid> TenantIds, IActionResult? Error)> ResolveScopeAsync(
        Guid tenantId, Guid? branchId, bool consolidated, CancellationToken ct)
    {
        if (consolidated)
        {
            var branchIds = await _context.Tenants
                .Where(t => t.ParentTenantId == tenantId && !t.IsDeleted)
                .Select(t => t.Id)
                .ToListAsync(ct);
            var ids = new List<Guid> { tenantId };
            ids.AddRange(branchIds);
            return (ids, null);
        }

        if (branchId.HasValue)
        {
            var isOwned = await _context.Tenants
                .AnyAsync(t => t.Id == branchId.Value && t.ParentTenantId == tenantId && !t.IsDeleted, ct);
            if (!isOwned)
                return (new List<Guid>(),
                        StatusCode(403, new { success = false, message = "Bu şube size ait değil." }));
            return (new List<Guid> { branchId.Value }, null);
        }

        return (new List<Guid> { tenantId }, null);
    }
}