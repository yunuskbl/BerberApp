using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Tenant.DTOs;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BerberApp.Application.Common.Interfaces;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _context;
    private readonly ILogger<SuperAdminController> _logger;

    // SuperAdmin'in sistem tenant ID'si
    private static readonly Guid SYSTEM_TENANT_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public SuperAdminController(IMediator mediator, IAppDbContext context, ILogger<SuperAdminController> logger)
    {
        _mediator = mediator;
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Tüm işletmeleri istatistikleri ile listele
    /// </summary>
    [HttpGet("tenants")]
    public async Task<IActionResult> GetAllTenantsWithStats()
    {
        try
        {
            var tenants = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Id != SYSTEM_TENANT_ID) // Sistem tenant'ını hariç tut
                .AsNoTracking()
                .Select(t => new SuperAdminTenantDto
                {
                    Id = t.Id,
                    Name = t.Name,
                    Subdomain = t.Subdomain,
                    LogoUrl = t.LogoUrl,
                    Phone = t.Phone,
                    Address = t.Address,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    StaffCount = t.Staff.Count(),
                    CustomerCount = t.Customers.Count(),
                    TotalAppointments = t.Appointments.Count(),
                    PendingAppointments = t.Appointments.Count(a => a.Status == AppointmentStatus.Pending),
                    CompletedAppointments = t.Appointments.Count(a => a.Status == AppointmentStatus.Completed),
                    Plan = "Basic" // TODO: Subscription model'den al
                })
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return Ok(new { success = true, data = tenants });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperAdmin tenants list error");
            return StatusCode(500, new { success = false, message = "Hata oluştu." });
        }
    }

    /// <summary>
    /// Yeni işletme ekle
    /// </summary>
    [HttpPost("tenants")]
    public async Task<IActionResult> CreateTenant([FromBody] RegisterCommand command)
    {
        try
        {
            var result = await _mediator.Send(command);
            return Ok(new { success = true, message = "İşletme oluşturuldu.", data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant creation error");
            return StatusCode(500, new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// İşletmeyi aktif/pasif yap
    /// </summary>
    [HttpPatch("tenants/{id}/toggle")]
    public async Task<IActionResult> ToggleTenantActive(Guid id)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == id && t.Id != SYSTEM_TENANT_ID);

            if (tenant == null)
                return NotFound(new { success = false, message = "İşletme bulunamadı." });

            tenant.IsActive = !tenant.IsActive;
            _context.Tenants.Update(tenant);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "İşletme durumu güncellendi.", data = new { id, isActive = tenant.IsActive } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Tenant toggle error");
            return StatusCode(500, new { success = false, message = "Hata oluştu." });
        }
    }
}
