using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BerberApp.Domain.Entities;


namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/booking")]
[AllowAnonymous]
public class BookingController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsApp;

    public BookingController(IMediator mediator, IAppDbContext context, IWhatsAppService whatsApp)
    {
        _mediator = mediator;
        _context = context;
        _whatsApp = whatsApp;
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

        var (monthlyCount, monthlyLimit) = await GetMonthlyAppointmentUsageAsync(tenant.Id);

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
                TotalReviews  = ratingData?.TotalReviews ?? 0,
                IsAtCapacity  = monthlyLimit < int.MaxValue && monthlyCount >= monthlyLimit,
                IsNearCapacity = monthlyLimit < int.MaxValue && monthlyCount >= (int)(monthlyLimit * 0.8) && monthlyCount < monthlyLimit
            }
        });
    }

    // Salona ait hizmetleri getir (opsiyonel: staffId ile filtrele)
    [HttpGet("{subdomain}/services")]
    public async Task<IActionResult> GetServices(string subdomain, [FromQuery] Guid? staffId)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var query = _context.Services.Where(x => x.TenantId == tenant.Id && x.IsActive);

        if (staffId.HasValue)
            query = query.Where(x => x.StaffServices.Any(ss => ss.StaffId == staffId.Value));

        var services = await query
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
                CustomDuration = staffId.HasValue
                    ? x.StaffServices
                        .Where(ss => ss.StaffId == staffId.Value)
                        .Select(ss => ss.CustomDurationMinutes)
                        .FirstOrDefault()
                    : null,
                CustomPrice = staffId.HasValue
                    ? x.StaffServices
                        .Where(ss => ss.StaffId == staffId.Value)
                        .Select(ss => ss.CustomPrice)
                        .FirstOrDefault()
                    : null,
                CustomCurrency = staffId.HasValue
                    ? x.StaffServices
                        .Where(ss => ss.StaffId == staffId.Value)
                        .Select(ss => ss.CustomCurrency)
                        .FirstOrDefault()
                    : null,
                HasOtherStaffPrices = x.StaffServices.Any(ss => ss.CustomPrice != null)
            })
            .ToListAsync();

        var result = services.Select(x => new
        {
            x.Id,
            x.Name,
            x.NameEn,
            x.NameRu,
            DurationMinutes = x.CustomDuration ?? x.DurationMinutes,
            Price = x.CustomPrice ?? x.Price,
            Currency = (x.CustomPrice.HasValue ? x.CustomCurrency : null) ?? x.Currency,
            x.Color,
            MinPrice = (decimal?)null,
            MaxPrice = (decimal?)null,
            HasMixedCurrencies = x.HasOtherStaffPrices && !staffId.HasValue
        });

        return Ok(new { success = true, data = result });
    }

    // Salona ait personeli getir (booking akışında servis filtresi yok)
    [HttpGet("{subdomain}/staff")]
    public async Task<IActionResult> GetStaff(string subdomain, [FromQuery] Guid? serviceId)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        var query = _context.Staff.Where(x => x.TenantId == tenant.Id && x.IsActive);

        if (serviceId.HasValue)
            query = query.Where(x => x.StaffServices.Any(ss => ss.ServiceId == serviceId.Value));

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
        [FromQuery] List<Guid>? serviceIds,
        [FromQuery] DateTime date)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null)
            return NotFound(new { success = false, message = "Salon bulunamadı." });

        // Çoklu hizmet varsa toplam süreyi hesapla
        int? totalDuration = null;
        var effectiveIds = serviceIds?.Count > 0 ? serviceIds : null;
        if (effectiveIds != null)
        {
            var services = await _context.Services
                .Where(s => effectiveIds.Contains(s.Id))
                .Select(s => new { s.Id, s.DurationMinutes })
                .ToListAsync();

            var customDurations = await _context.StaffServices
                .Where(ss => ss.StaffId == staffId && effectiveIds.Contains(ss.ServiceId) && ss.CustomDurationMinutes != null)
                .Select(ss => new { ss.ServiceId, ss.CustomDurationMinutes })
                .ToListAsync();

            totalDuration = services.Sum(s =>
            {
                var custom = customDurations.FirstOrDefault(cd => cd.ServiceId == s.Id);
                return custom?.CustomDurationMinutes ?? s.DurationMinutes;
            });
        }

        // İzin günü / salon kapalı gün kontrolü — boş liste döndür
        var requestDate = DateOnly.FromDateTime(date.ToUniversalTime());
        var isDayOff = await _context.StaffDaysOff
            .AnyAsync(d => d.StaffId == staffId && d.Date == requestDate && !d.IsDeleted);
        var isSalonClosed = await _context.TenantClosures
            .AnyAsync(c => c.TenantId == tenant.Id && !c.IsDeleted &&
                           c.StartDate <= requestDate && c.EndDate >= requestDate);
        if (isDayOff || isSalonClosed)
            return Ok(new { success = true, data = Array.Empty<object>() });

        var result = await _mediator.Send(new GetAvailableSlotsQuery
        {
            TenantId = tenant.Id,
            StaffId = staffId,
            ServiceId = effectiveIds?.FirstOrDefault() ?? serviceId,
            Date = date,
            TotalDurationMinutes = totalDuration
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

        // Aylık randevu limit kontrolü
        var (monthlyCount, monthlyLimit) = await GetMonthlyAppointmentUsageAsync(tenant.Id);
        if (monthlyLimit < int.MaxValue && monthlyCount >= monthlyLimit)
            return BadRequest(new { success = false, message = "Bu salon bu ay için randevu kapasitesine ulaştı." });

        // İzin günü / salon kapalı kontrolü
        var apptDate = DateOnly.FromDateTime(DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc));
        var isStaffDayOff = await _context.StaffDaysOff
            .AnyAsync(d => d.StaffId == request.StaffId && d.Date == apptDate && !d.IsDeleted);
        if (isStaffDayOff)
            return BadRequest(new { success = false, message = "Seçilen personel bu tarihte izinlidir." });
        var isSalonClosed = await _context.TenantClosures
            .AnyAsync(c => c.TenantId == tenant.Id && !c.IsDeleted &&
                           c.StartDate <= apptDate && c.EndDate >= apptDate);
        if (isSalonClosed)
            return BadRequest(new { success = false, message = "Salon bu tarihte kapalıdır." });

        // Telefon doğrulanmış mı kontrol et (son 30 dakika içinde doğrulanmış OTP)
        var verifiedOtp = await _context.OtpRecords
            .Where(x => x.Phone == request.Phone && x.IsVerified &&
                        x.VerifiedAt != null && x.VerifiedAt > DateTime.UtcNow.AddMinutes(-30))
            .FirstOrDefaultAsync();

        if (verifiedOtp is null)
            return BadRequest(new { success = false, message = "Telefon numarası doğrulanmamış." });

        // Kullanıldıktan sonra OTP kaydını sil
        _context.OtpRecords.Remove(verifiedOtp);
        await _context.SaveChangesAsync();

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

        // Çoklu hizmet desteği: toplam süre ve görüntüleme adı hesapla
        var effectiveServiceIds = request.ServiceIds?.Count > 0
            ? request.ServiceIds
            : new List<Guid> { request.ServiceId };

        int? totalDurationMinutes = null;
        string? serviceNamesDisplay = null;

        if (effectiveServiceIds.Count > 1)
        {
            var svcs = await _context.Services
                .Where(s => effectiveServiceIds.Contains(s.Id))
                .Select(s => new { s.Id, s.Name, s.DurationMinutes })
                .ToListAsync();

            var customDurs = await _context.StaffServices
                .Where(ss => ss.StaffId == request.StaffId && effectiveServiceIds.Contains(ss.ServiceId) && ss.CustomDurationMinutes != null)
                .Select(ss => new { ss.ServiceId, ss.CustomDurationMinutes })
                .ToListAsync();

            totalDurationMinutes = svcs.Sum(s =>
            {
                var cd = customDurs.FirstOrDefault(x => x.ServiceId == s.Id);
                return cd?.CustomDurationMinutes ?? s.DurationMinutes;
            });

            serviceNamesDisplay = string.Join(" + ", effectiveServiceIds
                .Select(id => svcs.FirstOrDefault(s => s.Id == id)?.Name)
                .Where(n => n != null));
        }

        var result = await _mediator.Send(new CreateAppointmentCommand
        {
            TenantId = tenant.Id,
            CustomerId = customer.Id,
            StaffId = request.StaffId,
            ServiceId = effectiveServiceIds.First(),
            StartTime = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc),
            Notes = request.Notes,
            IsFromBookingPage = true,
            NotificationPhone = tenant.NotificationPhone,
            TotalDurationMinutes = totalDurationMinutes,
            ServiceNamesDisplay = serviceNamesDisplay
        });

        // Tüm seçilen hizmetleri AppointmentServices tablosuna kaydet
        if (effectiveServiceIds.Count > 0)
        {
            try
            {
                var apptServices = effectiveServiceIds.Select(sid => new BerberApp.Domain.Entities.AppointmentService
                {
                    AppointmentId = result.Id,
                    ServiceId = sid
                });
                _context.AppointmentServices.AddRange(apptServices);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AppointmentServices HATA] {ex.Message}");
            }
        }

        // Uyarı eşiklerini kontrol et (sadece eşiği tam geçen anda gönder)
        if (monthlyLimit < int.MaxValue && !string.IsNullOrWhiteSpace(tenant.NotificationPhone))
        {
            var newCount = monthlyCount + 1;
            var warningAt80 = (int)(monthlyLimit * 0.8);
            if (newCount == warningAt80)
                _ = _whatsApp.SendMonthlyLimitWarningAsync(tenant.NotificationPhone, tenant.Name, newCount, monthlyLimit, false);
            else if (newCount == monthlyLimit)
                _ = _whatsApp.SendMonthlyLimitWarningAsync(tenant.NotificationPhone, tenant.Name, newCount, monthlyLimit, true);
        }

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
        public List<Guid>? ServiceIds { get; set; }
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

    private async Task<(int count, int limit)> GetMonthlyAppointmentUsageAsync(Guid tenantId)
    {
        var plan = await _context.Subscriptions
            .Where(s => s.TenantId == tenantId && s.Status == BerberApp.Domain.Enums.SubscriptionStatus.Active)
            .OrderByDescending(s => s.StartDate)
            .Select(s => s.Plan)
            .FirstOrDefaultAsync();

        var limit = plan switch
        {
            BerberApp.Domain.Enums.PlanType.Baslangic   => 100,
            BerberApp.Domain.Enums.PlanType.Profesyonel => 500,
            _                                            => int.MaxValue
        };

        if (limit == int.MaxValue) return (0, int.MaxValue);

        var turkeyTz = GetTurkeyTimeZone();
        var nowTurkey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
        var startOfMonth = new DateTime(nowTurkey.Year, nowTurkey.Month, 1);
        var startOfMonthUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(startOfMonth, DateTimeKind.Unspecified), turkeyTz);

        var count = await _context.Appointments
            .CountAsync(x => x.TenantId == tenantId &&
                             x.StartTime >= startOfMonthUtc &&
                             x.Status != AppointmentStatus.Cancelled);

        return (count, limit);
    }

    private static string NormalizePhone(string? p) =>
        (p ?? "").Replace(" ", "").Replace("-", "").Replace("(", "").Replace(")", "").Replace("+", "");

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }

}
