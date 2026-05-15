using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Api.Controllers;

public class AppointmentsController : BaseApiController
{
    private readonly IAppDbContext _context;
    private readonly IConfiguration _config;

    public AppointmentsController(IMediator mediator, IAppDbContext context, IConfiguration config)
        : base(mediator)
    {
        _context = context;
        _config  = config;
    }
    
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] Guid? staffId, [FromQuery] DateTime? date, [FromQuery] Guid? customerId)
        => Success(await Mediator.Send(new GetAllAppointmentsQuery
        {
            TenantId   = TenantId,
            StaffId    = staffId,
            Date       = date,
            CustomerId = customerId,
        }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Success(await Mediator.Send(new GetAppointmentByIdQuery
        {
            Id = id,
            TenantId = TenantId
        }));

    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots(
        [FromQuery] Guid staffId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateTime date)
        => Success(await Mediator.Send(new GetAvailableSlotsQuery
        {
            TenantId = TenantId,
            StaffId = staffId,
            ServiceId = serviceId,
            Date = date
        }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentCommand command)
    {
        command.TenantId = TenantId;
        command.IsFromBookingPage = false;
        return Created(await Mediator.Send(command));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateAppointmentCommand command)
    {
        command.Id       = id;
        command.TenantId = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpPut("{id}/confirm")]
    public async Task<IActionResult> Confirm(Guid id)
    {
        var result = await Mediator.Send(new ConfirmAppointmentCommand
        {
            Id = id,
            TenantId = TenantId
        });
        return Ok(new { success = true, data = result });
    }
    [HttpPatch("{id}/cancel")]
    public async Task<IActionResult> Cancel(Guid id)
    {
        await Mediator.Send(new CancelAppointmentCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }

    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> Complete(Guid id)
    {
        // Tenant subdomain'ini al → müşteriye gidecek değerlendirme linkini oluştur
        var tenantId = TenantId;

        string? subdomain = null;
        try
        {
            subdomain = await _context.Tenants
                .Where(t => t.Id == tenantId)
                .Select(t => t.Subdomain)
                .FirstOrDefaultAsync();
        }
        catch { /* subdomain alınamazsa reviewUrl olmadan devam et */ }

        var frontendBase = _config["AppSettings:FrontendBaseUrl"] ?? "https://ayarliyo.com";
        var reviewUrl    = subdomain is not null
            ? $"{frontendBase}/rate/{subdomain}/{id}"
            : null;

        await Mediator.Send(new CompleteAppointmentCommand
        {
            Id        = id,
            TenantId  = tenantId,
            ReviewUrl = reviewUrl
        });
        return Ok(new { success = true, message = "Randevu tamamlandı." });
    }

}
