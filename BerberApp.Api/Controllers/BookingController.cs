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

        var ratingData = await _context.Reviews
            .Where(r => r.TenantId == tenant.Id && !r.IsDeleted)
            .GroupBy(r => r.TenantId)
            .Select(g => new { AverageRating = g.Average(r => r.Rating), TotalReviews = g.Count() })
            .FirstOrDefaultAsync();

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
                photos,
                AverageRating = ratingData != null ? Math.Round(ratingData.AverageRating, 1) : 0.0,
                TotalReviews  = ratingData?.TotalReviews ?? 0
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
                x.Color,
                StaffPrices = x.StaffServices
                    .Where(ss => ss.CustomPrice != null)
                    .Select(ss => new { ss.CustomPrice, Currency = ss.CustomCurrency ?? x.Currency })
                    .ToList()
            })
            .ToListAsync();

        var result = services.Select(x =>
        {
            var hasMixedCurrencies = x.StaffPrices
                .Any(sp => sp.Currency != null && sp.Currency != x.Currency);
            decimal? minPrice, maxPrice;
            if (hasMixedCurrencies)
            {
                minPrice = x.Price;
                maxPrice = x.Price;
            }
            else
            {
                var staffPrices = x.StaffPrices
                    .Select(sp => sp.CustomPrice!.Value)
                    .Where(p => p > 0).ToList();
                if (staffPrices.Count > 0)
                {
                    minPrice = staffPrices.Min();
                    maxPrice = staffPrices.Max();
                }
                else
                {
                    minPrice = x.Price;
                    maxPrice = x.Price;
                }
            }
            return new
            {
                x.Id,
                x.Name,
                x.NameEn,
                x.NameRu,
                x.DurationMinutes,
                x.Price,
                x.Currency,
                x.Color,
                MinPrice = minPrice,
                MaxPrice = maxPrice
            };
        });

        return Ok(new { success = true, data = result });
    }

    // Salona ait personeli getir (opsiyonel: serviceId ile filtrele)
    [HttpGet("{subdomain}/staff")]
    public async Task<IActionResult> GetStaff(string subdomain, [FromQuery] Guid? serviceId)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var query = _context.Staff
            .Where(x => x.TenantId == tenant.Id && x.IsActive);

        if (serviceId.HasValue)
            query = query.Where(x => x.StaffServices.Any(ss => ss.ServiceId == serviceId.Value));

        if (serviceId.HasValue)
        {
            var sid = serviceId.Value;
            var servicePrice = await _context.Services
                .Where(s => s.Id == sid)
                .Select(s => new { s.Price, s.Currency })
                .FirstOrDefaultAsync();

            var staffWithPrice = await query
                .Select(x => new
                {
                    x.Id,
                    x.FullName,
                    x.AvatarUrl,
                    x.Bio,
                    CustomPrice = x.StaffServices
                        .Where(ss => ss.ServiceId == sid)
                        .Select(ss => ss.CustomPrice)
                        .FirstOrDefault(),
                    CustomCurrency = x.StaffServices
                        .Where(ss => ss.ServiceId == sid)
                        .Select(ss => ss.CustomCurrency)
                        .FirstOrDefault()
                })
                .ToListAsync();

            var result = staffWithPrice.Select(x => new
            {
                x.Id,
                x.FullName,
                x.AvatarUrl,
                x.Bio,
                Price = x.CustomPrice ?? servicePrice?.Price ?? 0,
                Currency = (x.CustomPrice.HasValue ? x.CustomCurrency : null) ?? servicePrice?.Currency ?? "TRY",
                HasCustomPrice = x.CustomPrice.HasValue
            });

            return Ok(new { success = true, data = result });
        }

        var staff = await query
            .Select(x => new { x.Id, x.FullName, x.AvatarUrl, x.Bio })
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

    // ── REVIEWS ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Randevu için değerlendirme gönder (müşteri, herkese açık).
    /// Her randevu için yalnızca bir değerlendirme kabul edilir.
    /// </summary>
    [HttpPost("{subdomain}/reviews/{appointmentId}")]
    public async Task<IActionResult> CreateReview(
        string subdomain,
        Guid appointmentId,
        [FromBody] CreateReviewRequest request)
    {
        if (request.Rating < 1 || request.Rating > 5)
            return BadRequest(new { success = false, message = "Puan 1-5 arasında olmalıdır." });

        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);
        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var appointment = await _context.Appointments
            .Where(x => x.Id == appointmentId && x.TenantId == tenant.Id)
            .FirstOrDefaultAsync();
        if (appointment is null)
            return NotFound(new { success = false, message = "Randevu bulunamadı." });

        if (appointment.Status != BerberApp.Domain.Enums.AppointmentStatus.Completed)
            return BadRequest(new { success = false, message = "Sadece tamamlanmış randevular değerlendirilebilir." });

        var alreadyReviewed = await _context.Reviews
            .AnyAsync(r => r.AppointmentId == appointmentId);
        if (alreadyReviewed)
            return Conflict(new { success = false, message = "Bu randevu için zaten değerlendirme yapılmış." });

        var customer = await _context.Customers
            .Where(x => x.Id == appointment.CustomerId)
            .FirstOrDefaultAsync();

        var review = new BerberApp.Domain.Entities.Review
        {
            TenantId      = tenant.Id,
            AppointmentId = appointmentId,
            CustomerId    = customer?.Id,
            CustomerName  = customer?.FullName ?? "Anonim",
            Rating        = request.Rating,
            Comment       = request.Comment?.Trim()
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Değerlendirmeniz için teşekkürler!" });
    }

    /// <summary>Salon için puanlama özetini döndürür (herkese açık).</summary>
    [HttpGet("{subdomain}/rating")]
    public async Task<IActionResult> GetRating(string subdomain)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);
        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var reviews = await _context.Reviews
            .Where(r => r.TenantId == tenant.Id && !r.IsDeleted)
            .Select(r => new { r.Rating, r.CustomerName, r.Comment, r.CreatedAt })
            .ToListAsync();

        var totalReviews   = reviews.Count;
        var averageRating  = totalReviews > 0 ? reviews.Average(r => r.Rating) : 0.0;
        var distribution   = Enumerable.Range(1, 5)
            .Select(i => new { star = i, count = reviews.Count(r => r.Rating == i) })
            .ToList();

        return Ok(new
        {
            success = true,
            data    = new { totalReviews, averageRating = Math.Round(averageRating, 1), distribution, reviews }
        });
    }

    public class CreateReviewRequest
    {
        public int    Rating  { get; set; }
        public string? Comment { get; set; }
    }

    // ── CUSTOMER LOOKUP ───────────────────────────────────────────────────────

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

        return Ok(new { success = true, data = new { customer.FullName } });
    }

    private static string NormalizePhone(string? p) =>
        (p ?? "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }

}
