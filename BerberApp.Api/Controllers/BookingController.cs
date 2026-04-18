using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/booking")]
[AllowAnonymous]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _context;

    public BookingController(IMediator mediator, IAppDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    // Salon bilgilerini getir
    [HttpGet("{subdomain}")]
    public async Task<IActionResult> GetSalon(string subdomain)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        return Ok(new
        {
            success = true,
            data = new
            {
                tenant.Id,
                tenant.Name,
                tenant.Phone,
                tenant.Address,
                tenant.LogoUrl
            }
        });
    }

    // Salona ait hizmetleri getir
    [HttpGet("{subdomain}/services")]
    public async Task<IActionResult> GetServices(string subdomain)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var services = await _context.Services
            .Where(x => x.TenantId == tenant.Id && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.DurationMinutes,
                x.Price,
                x.Currency,
                x.Color
            })
            .ToListAsync();

        return Ok(new { success = true, data = services });
    }

    // Salona ait personeli getir
    [HttpGet("{subdomain}/staff")]
    public async Task<IActionResult> GetStaff(string subdomain)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var staff = await _context.Staff
            .Where(x => x.TenantId == tenant.Id && x.IsActive)
            .Select(x => new
            {
                x.Id,
                x.FullName,
                x.AvatarUrl,
                x.Bio
            })
            .ToListAsync();

        return Ok(new { success = true, data = staff });
    }

    // Müsait slotları getir
    [HttpGet("{subdomain}/available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        string subdomain,
        [FromQuery] Guid staffId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime date)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var result = await _mediator.Send(new GetAvailableSlotsQuery
        {
            TenantId = tenant.Id,
            StaffId = staffId,
            ServiceId = serviceId,
            Date = date
        });

        return Ok(new { success = true, data = result });
    }

    // Randevu oluştur (müşteri)
    [HttpPost("{subdomain}/appointments")]
    public async Task<IActionResult> CreateAppointment(
        string subdomain,
        [FromBody] CustomerBookingRequest request)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        // Müşteriyi bul veya oluştur
        var customer = await _context.Customers
            .FirstOrDefaultAsync(x => x.Phone == request.Phone && x.TenantId == tenant.Id);

        if (customer is null)
        {
            customer = new BerberApp.Domain.Entities.Customer
            {
                TenantId = tenant.Id,
                FullName = request.FullName,
                Phone = request.Phone,
                Email = request.Email
            };
            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();
        }

        var result = await _mediator.Send(new CreateAppointmentCommand
        {
            TenantId = tenant.Id,
            CustomerId = customer.Id,
            StaffId = request.StaffId,
            ServiceId = request.ServiceId,
            StartTime = request.StartTime,
            Notes = request.Notes
        });

        return Ok(new { success = true, data = result });
    }
    public class CustomerBookingRequest
    {
        public string FullName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? Email { get; set; }
        public Guid StaffId { get; set; }
        public Guid ServiceId { get; set; }
        public DateTime StartTime { get; set; }
        public string? Notes { get; set; }
    }
}
