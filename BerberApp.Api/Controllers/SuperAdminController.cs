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
                .Where(t => t.Id != SYSTEM_TENANT_ID && !t.IsDeleted) // Sistem tenant'ı ve silinenleri hariç tut
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
    /// İşletme detayı
    /// </summary>
    [HttpGet("tenants/{id}")]
    public async Task<IActionResult> GetTenantDetail(Guid id)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Where(t => t.Id == id && t.Id != SYSTEM_TENANT_ID)
            .Select(t => new
            {
                t.Id, t.Name, t.Subdomain, t.Phone, t.Address, t.LogoUrl,
                t.IsActive, t.IsDeleted, t.CreatedAt,
                StaffCount = t.Staff.Count(),
                CustomerCount = t.Customers.Count(),
                TotalAppointments = t.Appointments.Count(),
                PendingAppointments = t.Appointments.Count(a => a.Status == AppointmentStatus.Pending),
                CompletedAppointments = t.Appointments.Count(a => a.Status == AppointmentStatus.Completed),
                CancelledAppointments = t.Appointments.Count(a => a.Status == AppointmentStatus.Cancelled),
                RecentAppointments = t.Appointments
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .Select(a => new {
                        a.Id, a.StartTime, a.Status,
                        CustomerName = a.Customer != null ? a.Customer.FullName : "—",
                        ServiceName = a.Service != null ? a.Service.Name : "—"
                    }).ToList()
            })
            .FirstOrDefaultAsync();

        if (tenant == null)
            return NotFound(new { success = false, message = "İşletme bulunamadı." });

        return Ok(new { success = true, data = tenant });
    }

    /// <summary>
    /// İşletmeyi aktif/pasif yap
    /// </summary>
    [HttpPatch("tenants/{id}/toggle")]
    public async Task<IActionResult> ToggleTenantActive(Guid id)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.Id != SYSTEM_TENANT_ID);

        if (tenant == null)
            return NotFound(new { success = false, message = "İşletme bulunamadı." });

        tenant.IsActive = !tenant.IsActive;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = $"İşletme {(tenant.IsActive ? "aktif" : "pasif")} yapıldı.", data = new { id, isActive = tenant.IsActive } });
    }

    /// <summary>
    /// Plan değiştir
    /// </summary>
    [HttpPatch("tenants/{id}/plan")]
    public async Task<IActionResult> ChangePlan(Guid id, [FromBody] ChangePlanRequest request)
    {
        if (!Enum.TryParse<BerberApp.Domain.Enums.PlanType>(request.Plan, ignoreCase: true, out var planType))
            return BadRequest(new { success = false, message = "Geçersiz plan." });

        var subscription = await _context.Subscriptions
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(s => s.TenantId == id);

        if (subscription == null)
        {
            // Subscription yoksa oluştur
            subscription = new BerberApp.Domain.Entities.Subscription
            {
                Id = Guid.NewGuid(),
                TenantId = id,
                Plan = planType,
                StartDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                Status = BerberApp.Domain.Enums.SubscriptionStatus.Active,
                Price = 0,
                Currency = "TRY",
                IsAutoRenewal = false,
                CreatedAt = DateTime.UtcNow
            };
            _context.Subscriptions.Add(subscription);
        }
        else
        {
            subscription.Plan = planType;
            subscription.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return Ok(new { success = true, message = "Plan güncellendi." });
    }

    /// <summary>
    /// Soft delete — işletmeyi pasif sil (geri alınabilir)
    /// </summary>
    [HttpDelete("tenants/{id}")]
    public async Task<IActionResult> SoftDeleteTenant(Guid id)
    {
        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == id && t.Id != SYSTEM_TENANT_ID);

        if (tenant == null)
            return NotFound(new { success = false, message = "İşletme bulunamadı." });

        tenant.IsDeleted = true;
        tenant.IsActive = false;
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "İşletme silindi (geri alınabilir)." });
    }

    /// <summary>
    /// Tenant verilerini sıfırla — randevu, müşteri ve personel verilerini siler
    /// </summary>
    [HttpPost("tenants/{id}/reset")]
    public async Task<IActionResult> ResetTenantData(Guid id)
    {
        try
        {
            // Tenant var mı kontrol et
            var tenantExists = await _context.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == id && t.Id != SYSTEM_TENANT_ID);

            if (!tenantExists)
                return NotFound(new { success = false, message = "İşletme bulunamadı." });

            // TenantId üzerinden direkt sorgula (Include/navigation property bypass)
            var appointments = await _context.Appointments
                .IgnoreQueryFilters()
                .Where(a => a.TenantId == id)
                .ToListAsync();

            var customers = await _context.Customers
                .IgnoreQueryFilters()
                .Where(c => c.TenantId == id)
                .ToListAsync();

            var staff = await _context.Staff
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == id)
                .ToListAsync();

            // Önce randevular (FK bağımlılıkları için)
            _context.Appointments.RemoveRange(appointments);
            await _context.SaveChangesAsync();

            // Sonra müşteri ve personel
            _context.Customers.RemoveRange(customers);
            _context.Staff.RemoveRange(staff);
            await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Tenant {TenantId} reset: {A} appointments, {C} customers, {S} staff deleted",
                id, appointments.Count, customers.Count, staff.Count);

            return Ok(new { success = true, message = "Veriler sıfırlandı.", deleted = new { appointments = appointments.Count, customers = customers.Count, staff = staff.Count } });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting tenant data for {TenantId}", id);
            return StatusCode(500, new { success = false, message = $"Sıfırlama başarısız: {ex.Message}" });
        }
    }

    /// <summary>
    /// Hard delete — işletmeyi kalıcı sil
    /// </summary>
    [HttpDelete("tenants/{id}/permanent")]
    public async Task<IActionResult> HardDeleteTenant(Guid id)
    {
        try
        {
            var tenant = await _context.Tenants
                .IgnoreQueryFilters()
                .Include(t => t.Appointments)
                .Include(t => t.Customers)
                .Include(t => t.Staff)
                .Include(t => t.Services)
                .Include(t => t.Users)
                .Include(t => t.Photos)
                .FirstOrDefaultAsync(t => t.Id == id && t.Id != SYSTEM_TENANT_ID);

            if (tenant == null)
                return NotFound(new { success = false, message = "İşletme bulunamadı." });

            // Delete related records in proper order (respecting foreign keys)
            // Delete appointments first
            _context.Appointments.RemoveRange(tenant.Appointments);

            // Delete customers (may have appointments references)
            _context.Customers.RemoveRange(tenant.Customers);

            // Delete staff
            _context.Staff.RemoveRange(tenant.Staff);

            // Delete services
            _context.Services.RemoveRange(tenant.Services);

            // Delete photos
            _context.TenantPhotos.RemoveRange(tenant.Photos);

            // Delete users
            _context.Users.RemoveRange(tenant.Users);

            // Finally delete the tenant itself
            _context.Tenants.Remove(tenant);

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Tenant {id} permanently deleted");
            return Ok(new { success = true, message = "İşletme kalıcı olarak silindi." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting tenant permanently");
            return StatusCode(500, new { success = false, message = $"Silme işlemi başarısız oldu: {ex.Message}" });
        }
    }
}

public class ChangePlanRequest
{
    public string Plan { get; set; } = string.Empty;
}
