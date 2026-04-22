using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;


namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/booking")]
[AllowAnonymous]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _context;
    private readonly IMemoryCache _cache;

    public BookingController(IMediator mediator, IAppDbContext context, IMemoryCache cache)
    {
        _mediator = mediator;
        _context = context;
        _cache = cache;
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

    [HttpPost("{subdomain}/appointments")]
    public async Task<IActionResult> CreateAppointment(
    string subdomain,
    [FromBody] CustomerBookingRequest request)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        // Telefon doğrulanmış mı kontrol et
        //if (!_cache.TryGetValue($"verified:{request.Phone}", out bool isVerified) || !isVerified)
        //    return BadRequest(new { success = false, message = "Telefon numarası doğrulanmamış." });
        // Telefon başına günlük limit kontrolü
        var turkeyTz = GetTurkeyTimeZone();
        var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
        var today = new DateTime(nowTurkey.Year, nowTurkey.Month, nowTurkey.Day, 0, 0, 0);
        var tomorrow = today.AddDays(1);
        var todayUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(today, DateTimeKind.Unspecified), turkeyTz);
        var tomorrowUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(tomorrow, DateTimeKind.Unspecified), turkeyTz);

        var phone = request.Phone.Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("0")) phone = "+90" + phone[1..];

        var existingCustomer = await _context.Customers
            .FirstOrDefaultAsync(x => x.Phone == request.Phone && x.TenantId == tenant.Id);

        if (existingCustomer != null)
        {
            var dailyBookings = await _context.Appointments
    .CountAsync(x => x.CustomerId == existingCustomer.Id &&
                     x.TenantId == tenant.Id &&
                     x.StartTime >= todayUtc &&  
                     x.StartTime < tomorrowUtc &&
                     x.Status != AppointmentStatus.Cancelled);

            if (dailyBookings >= 2)
                return BadRequest(new
                {
                    success = false,
                    message = "Bu telefon numarasıyla bugün için maksimum randevu sayısına ulaşıldı."
                });
        }


        // Müşteriyi bul veya oluştur
        var customer = existingCustomer;
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
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),  
            Notes = request.Notes,
            IsFromBookingPage = true
        });

        return Ok(new { success = true, data = result, appointmentId = result.Id });
    }

    [HttpGet("{subdomain}/appointments/{appointmentId}")]
    public async Task<IActionResult> GetAppointmentStatus(string subdomain, Guid appointmentId)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var result = await _mediator.Send(new GetAppointmentStatusQuery
        {
            AppointmentId = appointmentId,
            TenantId = tenant.Id
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

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }

}
