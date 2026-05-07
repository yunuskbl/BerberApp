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

        var photos = await _context.TenantPhotos
            .Where(x => x.TenantId == tenant.Id)
            .OrderBy(x => x.Order)
            .Select(x => new { x.Id, x.Url })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            data = new
            {
                tenant.Id,
                tenant.Name,
                tenant.Phone,
                tenant.Address,
                tenant.LogoUrl,
                tenant.ThemeColor,
                photos
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
                x.NameEn,
                x.NameRu,
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
        if (!_cache.TryGetValue($"verified:{request.Phone}", out bool isVerified) || !isVerified)
            return BadRequest(new { success = false, message = "Telefon numarası doğrulanmamış." });

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
            IsFromBookingPage = true,
            NotificationPhone = tenant.NotificationPhone
        });

        return Ok(new { success = true, data = result, appointmentId = result.Id });
    }

    [HttpGet("{subdomain}/appointments/{appointmentId}")]
    public async Task<IActionResult> GetAppointmentStatus(
        string subdomain,
        Guid appointmentId,
        [FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { success = false, message = "Telefon numarası gerekli." });

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var appt = await _context.Appointments
            .Where(x => x.Id == appointmentId && x.TenantId == tenant.Id)
            .Select(x => new { x.CustomerId })
            .FirstOrDefaultAsync();

        if (appt is null)
            return NotFound(new { success = false, message = "Randevu bulunamadı." });

        var customerPhone = await _context.Customers
            .Where(x => x.Id == appt.CustomerId)
            .Select(x => x.Phone)
            .FirstOrDefaultAsync();

        static string Normalize(string? p) =>
            (p ?? "").Replace(" ", "").Replace("-", "").TrimStart('0');

        if (Normalize(customerPhone) != Normalize(phone))
            return Unauthorized(new { success = false, message = "Bu randevuya erişim yetkiniz yok." });

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

    [HttpPost("{subdomain}/appointments/{appointmentId}/cancel")]
    public async Task<IActionResult> CancelAppointment(
        string subdomain,
        Guid appointmentId,
        [FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return BadRequest(new { success = false, message = "Telefon numarası gerekli." });

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var appt = await _context.Appointments
            .Where(x => x.Id == appointmentId && x.TenantId == tenant.Id)
            .Select(x => new { x.CustomerId, x.Status, x.StartTime })
            .FirstOrDefaultAsync();

        if (appt is null)
            return NotFound(new { success = false, message = "Randevu bulunamadı." });

        var customerPhone = await _context.Customers
            .Where(x => x.Id == appt.CustomerId)
            .Select(x => x.Phone)
            .FirstOrDefaultAsync();

        static string Normalize(string? p) =>
            (p ?? "").Replace(" ", "").Replace("-", "").TrimStart('0');

        if (Normalize(customerPhone) != Normalize(phone))
            return Unauthorized(new { success = false, message = "Bu randevuya erişim yetkiniz yok." });

        if (appt.Status != AppointmentStatus.Confirmed)
            return BadRequest(new { success = false, message = "Sadece onaylanmış randevular iptal edilebilir." });

        var turkeyTz = GetTurkeyTimeZone();
        var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
        var startTurkey = TimeZoneInfo.ConvertTimeFromUtc(appt.StartTime, turkeyTz);

        if ((startTurkey - nowTurkey).TotalHours < 2)
            return BadRequest(new { success = false, message = "Randevunuzu iptal etmek için en az 2 saat öncesinde talepte bulunmalısınız." });

        await _mediator.Send(new CancelAppointmentCommand
        {
            Id = appointmentId,
            TenantId = tenant.Id
        });

        return Ok(new { success = true, message = "Randevunuz iptal edildi." });
    }

    [HttpGet("{subdomain}/customer-lookup")]
    public async Task<IActionResult> CustomerLookup(string subdomain, [FromQuery] string phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return Ok(new { success = false });

        var normalized = NormalizePhone(phone);
        if (normalized.Length < 10)
            return Ok(new { success = false });

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return Ok(new { success = false });

        var customers = await _context.Customers
            .Where(x => x.TenantId == tenant.Id)
            .Select(x => new { x.FullName, x.Email, x.Notes, x.Phone })
            .ToListAsync();

        var customer = customers.FirstOrDefault(x => NormalizePhone(x.Phone) == normalized);

        if (customer is null)
            return Ok(new { success = false });

        return Ok(new { success = true, data = new { customer.FullName, customer.Email } });
    }

    private static string NormalizePhone(string? p) =>
        (p ?? "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }

}
