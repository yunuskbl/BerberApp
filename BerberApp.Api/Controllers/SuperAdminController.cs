using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Tenant.DTOs;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;

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
            var now = DateTime.UtcNow;
            var tenants = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Id != SYSTEM_TENANT_ID && !t.IsDeleted)
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
                    AdminEmail = t.Users
                        .Where(u => u.Role == UserRole.Admin)
                        .Select(u => u.Email)
                        .FirstOrDefault(),
                    AdminName = t.Users
                        .Where(u => u.Role == UserRole.Admin)
                        .Select(u => u.FirstName + " " + u.LastName)
                        .FirstOrDefault(),
                    StaffCount = t.Staff.Count(),
                    CustomerCount = t.Customers.Count(),
                    TotalAppointments = t.Appointments.Count(),
                    PendingAppointments = t.Appointments.Count(a => a.Status == AppointmentStatus.Pending),
                    CompletedAppointments = t.Appointments.Count(a => a.Status == AppointmentStatus.Completed),
                    Plan = _context.Subscriptions
                        .Where(s => s.TenantId == t.Id)
                        .OrderByDescending(s => s.StartDate)
                        .Select(s => s.Plan.ToString())
                        .FirstOrDefault() ?? "Baslangic",
                    SubscriptionStatus = _context.Subscriptions
                        .Where(s => s.TenantId == t.Id)
                        .OrderByDescending(s => s.StartDate)
                        .Select(s => s.Status.ToString())
                        .FirstOrDefault() ?? "None",
                    SubscriptionExpiresAt = _context.Subscriptions
                        .Where(s => s.TenantId == t.Id)
                        .OrderByDescending(s => s.StartDate)
                        .Select(s => (DateTime?)s.ExpiryDate)
                        .FirstOrDefault(),
                    IsOnTrial = _context.Subscriptions
                        .Any(s => s.TenantId == t.Id && s.Status == SubscriptionStatus.Trial && s.ExpiryDate > now),
                    DaysLeft = _context.Subscriptions
                        .Where(s => s.TenantId == t.Id && s.ExpiryDate > now)
                        .OrderByDescending(s => s.StartDate)
                        .Select(s => (int?)EF.Functions.DateDiffDay(now, s.ExpiryDate))
                        .FirstOrDefault()
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
    /// Sistem geneli raporlar
    /// </summary>
    [HttpGet("reports")]
    public async Task<IActionResult> GetReports()
    {
        try
        {
            var now = DateTime.UtcNow;
            var thirtyDaysAgo = now.AddDays(-30);
            var sevenDaysAgo = now.AddDays(-7);

            var totalTenants = await _context.Tenants
                .IgnoreQueryFilters()
                .CountAsync(t => t.Id != SYSTEM_TENANT_ID && !t.IsDeleted);

            var activeTenants = await _context.Tenants
                .IgnoreQueryFilters()
                .CountAsync(t => t.Id != SYSTEM_TENANT_ID && !t.IsDeleted && t.IsActive);

            var trialTenants = await _context.Subscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Trial && s.ExpiryDate > now);

            var activePaidTenants = await _context.Subscriptions
                .CountAsync(s => s.Status == SubscriptionStatus.Active && s.ExpiryDate > now);

            var newThisMonth = await _context.Tenants
                .IgnoreQueryFilters()
                .CountAsync(t => t.Id != SYSTEM_TENANT_ID && !t.IsDeleted && t.CreatedAt >= thirtyDaysAgo);

            var newThisWeek = await _context.Tenants
                .IgnoreQueryFilters()
                .CountAsync(t => t.Id != SYSTEM_TENANT_ID && !t.IsDeleted && t.CreatedAt >= sevenDaysAgo);

            var totalAppointments = await _context.Appointments.CountAsync();
            var appointmentsThisMonth = await _context.Appointments
                .CountAsync(a => a.CreatedAt >= thirtyDaysAgo);

            var planDist = await _context.Subscriptions
                .Where(s => s.ExpiryDate > now)
                .GroupBy(s => s.Plan)
                .Select(g => new { Plan = g.Key.ToString(), Count = g.Count() })
                .ToListAsync();

            // Son 6 ayın kayıt trendi
            var monthlyGrowth = await _context.Tenants
                .IgnoreQueryFilters()
                .Where(t => t.Id != SYSTEM_TENANT_ID && !t.IsDeleted && t.CreatedAt >= now.AddMonths(-6))
                .GroupBy(t => new { t.CreatedAt.Year, t.CreatedAt.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var expiringSoon = await _context.Subscriptions
                .Where(s => s.ExpiryDate > now && s.ExpiryDate <= now.AddDays(7))
                .CountAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    totalTenants,
                    activeTenants,
                    trialTenants,
                    activePaidTenants,
                    newThisMonth,
                    newThisWeek,
                    totalAppointments,
                    appointmentsThisMonth,
                    expiringSoon,
                    planDistribution = planDist,
                    monthlyGrowth
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperAdmin reports error");
            return StatusCode(500, new { success = false, message = "Hata oluştu." });
        }
    }

    /// <summary>
    /// Tüm abonelikler / ödeme takibi
    /// </summary>
    [HttpGet("subscriptions")]
    public async Task<IActionResult> GetAllSubscriptions([FromQuery] string? status = null)
    {
        try
        {
            var now = DateTime.UtcNow;
            var query = _context.Subscriptions
                .IgnoreQueryFilters()
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse<SubscriptionStatus>(status, true, out var s))
                    query = query.Where(sub => sub.Status == s);
            }

            var subs = await query
                .OrderByDescending(s => s.CreatedAt)
                .Select(s => new
                {
                    s.Id,
                    s.TenantId,
                    TenantName = _context.Tenants
                        .IgnoreQueryFilters()
                        .Where(t => t.Id == s.TenantId)
                        .Select(t => t.Name)
                        .FirstOrDefault(),
                    AdminEmail = _context.Users
                        .Where(u => u.TenantId == s.TenantId && u.Role == UserRole.Admin)
                        .Select(u => u.Email)
                        .FirstOrDefault(),
                    Plan = s.Plan.ToString(),
                    Status = s.Status.ToString(),
                    s.StartDate,
                    s.ExpiryDate,
                    s.Price,
                    s.Currency,
                    s.CreatedAt,
                    IsExpired = s.ExpiryDate <= now,
                    DaysLeft = s.ExpiryDate > now
                        ? (int?)EF.Functions.DateDiffDay(now, s.ExpiryDate)
                        : null
                })
                .ToListAsync();

            return Ok(new { success = true, data = subs });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SuperAdmin subscriptions error");
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
                    }).ToList(),
                Plan = _context.Subscriptions
                    .Where(s => s.TenantId == t.Id
                             && s.Status == SubscriptionStatus.Active
                             && s.ExpiryDate > DateTime.UtcNow)
                    .OrderByDescending(s => s.StartDate)
                    .Select(s => s.Plan.ToString())
                    .FirstOrDefault() ?? "Baslangic"
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
            subscription.Status = BerberApp.Domain.Enums.SubscriptionStatus.Active;
            subscription.ExpiryDate = DateTime.UtcNow.AddYears(1);
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
            var tenantExists = await _context.Tenants
                .IgnoreQueryFilters()
                .AnyAsync(t => t.Id == id && t.Id != SYSTEM_TENANT_ID);

            if (!tenantExists)
                return NotFound(new { success = false, message = "İşletme bulunamadı." });

            // ExecuteDeleteAsync → direkt SQL DELETE, change tracker bypass, FK sırası önemli
            var deletedAppointments = await _context.Appointments
                .IgnoreQueryFilters()
                .Where(a => a.TenantId == id)
                .ExecuteDeleteAsync();

            var deletedCustomers = await _context.Customers
                .IgnoreQueryFilters()
                .Where(c => c.TenantId == id)
                .ExecuteDeleteAsync();

            var deletedStaff = await _context.Staff
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == id)
                .ExecuteDeleteAsync();

            _logger.LogInformation(
                "Tenant {TenantId} reset: {A} appointments, {C} customers, {S} staff deleted",
                id, deletedAppointments, deletedCustomers, deletedStaff);

            return Ok(new { success = true, message = "Veriler sıfırlandı." });
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
