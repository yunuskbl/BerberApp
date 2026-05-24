using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using BerberApp.Application.Staff.Queries;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

public class StaffController : BaseApiController
{
    private readonly IAppDbContext _context;

    public StaffController(IMediator mediator, IAppDbContext context) : base(mediator)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Success(await Mediator.Send(new GetAllStaffQuery { TenantId = TenantId }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Success(await Mediator.Send(new GetStaffByIdQuery { Id = id, TenantId = TenantId }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateStaffCommand command)
    {
        command.TenantId = TenantId;
        return Created(await Mediator.Send(command));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateStaffCommand command)
    {
        command.Id = id;
        command.TenantId = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteStaffCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }

    [HttpGet("{id}/services")]
    public async Task<IActionResult> GetServices(Guid id)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        var staffServices = await _context.StaffServices
            .Where(ss => ss.StaffId == id)
            .Select(ss => new { ss.ServiceId, ss.CustomPrice, ss.CustomCurrency, ss.CustomDurationMinutes })
            .ToListAsync();

        return Success(staffServices);
    }

    [HttpPut("{id}/services")]
    public async Task<IActionResult> SetServices(Guid id, [FromBody] SetStaffServicesRequest request)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        request = request with { Items = request.Items ?? new List<StaffServiceItem>() };

        var validServiceIds = await _context.Services
            .Where(s => request.Items.Select(i => i.ServiceId).Contains(s.Id) && s.TenantId == TenantId)
            .Select(s => s.Id)
            .ToListAsync();

        var existing = await _context.StaffServices
            .Where(ss => ss.StaffId == id)
            .ToListAsync();

        _context.StaffServices.RemoveRange(existing);

        foreach (var item in request.Items.Where(i => validServiceIds.Contains(i.ServiceId)))
        {
            _context.StaffServices.Add(new BerberApp.Domain.Entities.StaffService
            {
                StaffId = id,
                ServiceId = item.ServiceId,
                CustomPrice = item.CustomPrice,
                CustomCurrency = item.CustomCurrency,
                CustomDurationMinutes = item.CustomDurationMinutes,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
        return Success("Hizmet atamaları güncellendi.");
    }

    public record StaffServiceItem(Guid ServiceId, decimal? CustomPrice, string? CustomCurrency, int? CustomDurationMinutes);
    public record SetStaffServicesRequest(List<StaffServiceItem> Items);

    // ── İzin Günleri ──────────────────────────────────────────────────────────

    [HttpGet("{id}/days-off")]
    public async Task<IActionResult> GetDaysOff(Guid id)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        var daysOff = await _context.StaffDaysOff
            .Where(d => d.StaffId == id && !d.IsDeleted)
            .OrderBy(d => d.Date)
            .Select(d => new { d.Id, d.StaffId, Date = d.Date.ToString("yyyy-MM-dd"), d.Reason })
            .ToListAsync();

        return Success(daysOff);
    }

    [HttpPost("{id}/days-off")]
    public async Task<IActionResult> AddDayOff(Guid id, [FromBody] AddDayOffRequest request)
    {
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        if (!DateOnly.TryParse(request.Date, out var date))
            return BadRequest(new { success = false, message = "Geçersiz tarih formatı." });

        var dayOff = new BerberApp.Domain.Entities.StaffDayOff
        {
            StaffId = id,
            Date    = date,
            Reason  = request.Reason,
        };

        _context.StaffDaysOff.Add(dayOff);
        await _context.SaveChangesAsync();

        return Created(new { dayOff.Id, dayOff.StaffId, Date = dayOff.Date.ToString("yyyy-MM-dd"), dayOff.Reason });
    }

    [HttpDelete("{id}/days-off/{dayOffId}")]
    public async Task<IActionResult> DeleteDayOff(Guid id, Guid dayOffId)
    {
        var dayOff = await _context.StaffDaysOff
            .FirstOrDefaultAsync(d => d.Id == dayOffId && d.StaffId == id && !d.IsDeleted);

        if (dayOff is null)
            return NotFound(new { success = false, message = "İzin günü bulunamadı." });

        // TenantId kontrolü — başka tenant'ın personeli değil
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        _context.StaffDaysOff.Remove(dayOff);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public record AddDayOffRequest(string Date, string? Reason);

    // ── Personel Hesabı Oluşturma ────────────────────────────────────────────

    [HttpPost("{id}/create-account")]
    [Authorize(Roles = "Admin,SuperAdmin")]
    public async Task<IActionResult> CreateAccount(Guid id, [FromBody] CreateStaffAccountRequest request)
    {
        // Personelin bu tenant'a ait olduğunu doğrula
        var staff = await _context.Staff
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        // Bu personele zaten bir hesap bağlı mı?
        var existingByStaff = await _context.Users
            .FirstOrDefaultAsync(u => u.StaffId == id && !u.IsDeleted);
        if (existingByStaff is not null)
            return BadRequest(new { success = false, message = "Bu personele zaten bir giriş hesabı tanımlı." });

        // E-posta zaten kayıtlı mı?
        var existingByEmail = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email.ToLower().Trim() && !u.IsDeleted);
        if (existingByEmail is not null)
            return BadRequest(new { success = false, message = "Bu e-posta adresi zaten kullanımda." });

        var user = new User
        {
            Id           = Guid.NewGuid(),
            TenantId     = TenantId,
            StaffId      = staff.Id,
            Email        = request.Email.ToLower().Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            FirstName    = staff.FullName.Split(' ')[0],
            LastName     = staff.FullName.Contains(' ') ? staff.FullName[(staff.FullName.IndexOf(' ') + 1)..] : "",
            Phone        = staff.Phone,
            Role         = UserRole.Staff,
            IsVerified   = true,
            CreatedAt    = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Created(new { email = user.Email, staffId = staff.Id, message = "Personel giriş hesabı oluşturuldu." });
    }

    // Bu personele hesap tanımlı mı?
    [HttpGet("{id}/has-account")]
    public async Task<IActionResult> HasAccount(Guid id)
    {
        var has = await _context.Users.AnyAsync(u => u.StaffId == id && !u.IsDeleted);
        return Success(new { hasAccount = has });
    }

    public record CreateStaffAccountRequest(string Email, string Password);
}
