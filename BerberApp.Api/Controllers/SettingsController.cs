using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

public class SettingsController : BaseApiController
{
    private readonly IAppDbContext _context;

    public SettingsController(IMediator mediator, IAppDbContext context) : base(mediator)
    {
        _context = context;
    }

    [HttpGet("notification-channel")]
    public async Task<IActionResult> GetNotificationChannel()
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == TenantId);

        if (tenant is null)
            return NotFound(new { success = false, message = "Tenant bulunamadı." });

        return Success(new { channel = (int)tenant.PreferredNotificationChannel });
    }

    [HttpPut("notification-channel")]
    public async Task<IActionResult> UpdateNotificationChannel([FromBody] UpdateNotificationChannelRequest request)
    {
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.Id == TenantId);

        if (tenant is null)
            return NotFound(new { success = false, message = "Tenant bulunamadı." });

        tenant.PreferredNotificationChannel = request.Channel;
        await _context.SaveChangesAsync();

        return Success(new { channel = (int)tenant.PreferredNotificationChannel });
    }
}

public class UpdateNotificationChannelRequest
{
    public NotificationChannel Channel { get; set; }
}
