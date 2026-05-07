using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using BerberApp.Application.Staff.Queries;
using MediatR;
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
            .Include(s => s.Services)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        var serviceIds = staff.Services.Select(s => s.Id).ToList();
        return Success(serviceIds);
    }

    [HttpPut("{id}/services")]
    public async Task<IActionResult> SetServices(Guid id, [FromBody] SetStaffServicesRequest request)
    {
        var staff = await _context.Staff
            .Include(s => s.Services)
            .FirstOrDefaultAsync(s => s.Id == id && s.TenantId == TenantId);

        if (staff is null)
            return NotFound(new { success = false, message = "Personel bulunamadı." });

        var services = await _context.Services
            .Where(s => request.ServiceIds.Contains(s.Id) && s.TenantId == TenantId)
            .ToListAsync();

        staff.Services.Clear();
        foreach (var svc in services)
            staff.Services.Add(svc);

        await _context.SaveChangesAsync();
        return Success("Hizmet atamaları güncellendi.");
    }

    public record SetStaffServicesRequest(List<Guid> ServiceIds);
}
