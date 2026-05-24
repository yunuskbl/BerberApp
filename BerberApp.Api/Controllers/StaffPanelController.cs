using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

/// <summary>
/// Personel girişi yapan kullanıcıların kendi verilerini gördüğü endpoint'ler.
/// Sadece Role = Staff olan tokenlar erişebilir.
/// </summary>
[Authorize(Roles = "Staff")]
public class StaffPanelController : BaseApiController
{
    private readonly IAppDbContext _context;

    public StaffPanelController(IMediator mediator, IAppDbContext context) : base(mediator)
    {
        _context = context;
    }

    // ── Randevularım ─────────────────────────────────────────────────────────

    [HttpGet("appointments")]
    public async Task<IActionResult> GetMyAppointments([FromQuery] DateTime? date)
    {
        var staffId = CurrentStaffId;
        if (staffId is null)
            return BadRequest(new { success = false, message = "Personel bilgisi bulunamadı." });

        var query = _context.Appointments
            .Include(a => a.Customer)
            .Include(a => a.Service)
            .Where(a => a.StaffId == staffId.Value &&
                        a.TenantId == TenantId &&
                        !a.IsDeleted);

        if (date.HasValue)
        {
            var dayStart = date.Value.Date;
            var dayEnd   = dayStart.AddDays(1);
            query = query.Where(a => a.StartTime >= dayStart && a.StartTime < dayEnd);
        }

        var appointments = await query
            .OrderBy(a => a.StartTime)
            .Select(a => new
            {
                a.Id,
                a.StartTime,
                a.EndTime,
                a.Status,
                CustomerName  = a.Customer.FullName,
                CustomerPhone = a.Customer.Phone,
                ServiceName   = a.Service.Name,
                a.Notes,
            })
            .ToListAsync();

        return Success(appointments);
    }

    // ── Çalışma Saatlerim ────────────────────────────────────────────────────

    [HttpGet("working-hours")]
    public async Task<IActionResult> GetMyWorkingHours()
    {
        var staffId = CurrentStaffId;
        if (staffId is null)
            return BadRequest(new { success = false, message = "Personel bilgisi bulunamadı." });

        var hours = await _context.WorkingHours
            .Where(wh => wh.StaffId == staffId.Value && !wh.IsDeleted)
            .OrderBy(wh => wh.DayOfWeek)
            .Select(wh => new
            {
                wh.Id,
                wh.DayOfWeek,
                wh.StartTime,
                wh.EndTime,
                wh.IsOff,
            })
            .ToListAsync();

        return Success(hours);
    }

    // ── İzin Günlerim ────────────────────────────────────────────────────────

    [HttpGet("days-off")]
    public async Task<IActionResult> GetMyDaysOff()
    {
        var staffId = CurrentStaffId;
        if (staffId is null)
            return BadRequest(new { success = false, message = "Personel bilgisi bulunamadı." });

        var daysOff = await _context.StaffDaysOff
            .Where(d => d.StaffId == staffId.Value && !d.IsDeleted)
            .OrderBy(d => d.Date)
            .Select(d => new { d.Id, d.StaffId, Date = d.Date.ToString("yyyy-MM-dd"), d.Reason })
            .ToListAsync();

        return Success(daysOff);
    }

    [HttpPost("days-off")]
    public async Task<IActionResult> AddMyDayOff([FromBody] AddDayOffRequest request)
    {
        var staffId = CurrentStaffId;
        if (staffId is null)
            return BadRequest(new { success = false, message = "Personel bilgisi bulunamadı." });

        if (!DateOnly.TryParse(request.Date, out var date))
            return BadRequest(new { success = false, message = "Geçersiz tarih formatı." });

        var dayOff = new BerberApp.Domain.Entities.StaffDayOff
        {
            StaffId = staffId.Value,
            Date    = date,
            Reason  = request.Reason,
        };

        _context.StaffDaysOff.Add(dayOff);
        await _context.SaveChangesAsync();

        return Created(new { dayOff.Id, dayOff.StaffId, Date = dayOff.Date.ToString("yyyy-MM-dd"), dayOff.Reason });
    }

    [HttpDelete("days-off/{dayOffId}")]
    public async Task<IActionResult> DeleteMyDayOff(Guid dayOffId)
    {
        var staffId = CurrentStaffId;
        if (staffId is null)
            return BadRequest(new { success = false, message = "Personel bilgisi bulunamadı." });

        var dayOff = await _context.StaffDaysOff
            .FirstOrDefaultAsync(d => d.Id == dayOffId && d.StaffId == staffId.Value && !d.IsDeleted);

        if (dayOff is null)
            return NotFound(new { success = false, message = "İzin günü bulunamadı." });

        _context.StaffDaysOff.Remove(dayOff);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    // ── Ben kimim? ───────────────────────────────────────────────────────────

    [HttpGet("me")]
    public async Task<IActionResult> GetMe()
    {
        var staffId = CurrentStaffId;
        if (staffId is null)
            return BadRequest(new { success = false, message = "Personel bilgisi bulunamadı." });

        var staff = await _context.Staff
            .Where(s => s.Id == staffId.Value && s.TenantId == TenantId && !s.IsDeleted)
            .Select(s => new { s.Id, s.FullName, s.Phone, s.AvatarUrl, s.Bio })
            .FirstOrDefaultAsync();

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        return Success(staff);
    }

    public record AddDayOffRequest(string Date, string? Reason);
}
