using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BerberApp.Api.Controllers;

public record CompleteAppointmentRequest(
    List<Guid> ActualServiceIds,
    decimal? ActualTotalPrice,
    string? CompletionNotes);

public record CreateAppointmentAdminRequest(
    Guid CustomerId,
    Guid StaffId,
    List<Guid> ServiceIds,
    DateTime StartTime,
    string? Notes);

[Authorize(Roles = "Admin,SuperAdmin")]
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
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? staffId,
        [FromQuery] DateTime? date,
        [FromQuery] Guid? customerId,
        [FromQuery] BerberApp.Domain.Enums.AppointmentStatus? status)
        => Success(await Mediator.Send(new GetAllAppointmentsQuery
        {
            TenantId   = TenantId,
            StaffId    = staffId,
            Date       = date,
            CustomerId = customerId,
            Status     = status,
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
    public async Task<IActionResult> Create([FromBody] CreateAppointmentAdminRequest request)
    {
        if (request.ServiceIds is null || request.ServiceIds.Count == 0)
            return BadRequest(new { message = "En az bir hizmet seçilmesi zorunludur." });

        int? totalDurationMinutes = null;
        string? serviceNamesDisplay = null;

        var svcs = await _context.Services
            .Where(s => request.ServiceIds.Contains(s.Id) && s.TenantId == TenantId)
            .Select(s => new { s.Id, s.Name, s.DurationMinutes })
            .ToListAsync();

        if (request.ServiceIds.Count > 1)
        {
            var customDurs = await _context.StaffServices
                .Where(ss => ss.StaffId == request.StaffId &&
                             request.ServiceIds.Contains(ss.ServiceId) &&
                             ss.CustomDurationMinutes != null)
                .Select(ss => new { ss.ServiceId, ss.CustomDurationMinutes })
                .ToListAsync();

            totalDurationMinutes = svcs.Sum(s =>
            {
                var cd = customDurs.FirstOrDefault(x => x.ServiceId == s.Id);
                return cd?.CustomDurationMinutes ?? s.DurationMinutes;
            });

            serviceNamesDisplay = string.Join(" + ", request.ServiceIds
                .Select(id => svcs.FirstOrDefault(s => s.Id == id)?.Name)
                .Where(n => n != null));
        }

        var command = new CreateAppointmentCommand
        {
            TenantId             = TenantId,
            CustomerId           = request.CustomerId,
            StaffId              = request.StaffId,
            ServiceId            = request.ServiceIds.First(),
            StartTime            = request.StartTime,
            Notes                = request.Notes,
            IsFromBookingPage    = false,
            TotalDurationMinutes = totalDurationMinutes,
            ServiceNamesDisplay  = serviceNamesDisplay,
        };

        var result = await Mediator.Send(command);

        if (request.ServiceIds.Count > 0)
        {
            var apptServices = request.ServiceIds.Select(sid =>
                new BerberApp.Domain.Entities.AppointmentService
                {
                    AppointmentId = result.Id,
                    ServiceId     = sid
                });
            _context.AppointmentServices.AddRange(apptServices);
            await _context.SaveChangesAsync();
        }

        return Created(result);
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
    public async Task<IActionResult> Complete(Guid id, [FromBody] CompleteAppointmentRequest? body)
    {
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

        var result = await Mediator.Send(new CompleteAppointmentCommand
        {
            Id               = id,
            TenantId         = tenantId,
            ReviewUrl        = reviewUrl,
            ActualServiceIds = body?.ActualServiceIds ?? new(),
            ActualTotalPrice = body?.ActualTotalPrice,
            CompletionNotes  = body?.CompletionNotes
        });
        return Success(new { receiptNumber = result.ReceiptNumber }, "Randevu tamamlandı.");
    }

}
