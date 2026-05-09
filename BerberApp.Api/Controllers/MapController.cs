using BerberApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

/// <summary>
/// WhatsApp mesajlarında kısa /map/{subdomain} linki gösterilir.
/// Bu endpoint tenant adresine göre Google Maps'e yönlendirir.
/// </summary>
[ApiController]
[Route("map")]
[AllowAnonymous]
public class MapController : ControllerBase
{
    private readonly IAppDbContext _context;

    public MapController(IAppDbContext context) => _context = context;

    [HttpGet("{subdomain}")]
    public async Task<IActionResult> Redirect(string subdomain)
    {
        var tenant = await _context.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Subdomain == subdomain && x.IsActive);

        if (tenant is null || string.IsNullOrWhiteSpace(tenant.Address))
            return NotFound("Adres bulunamadı.");

        var mapsUrl = $"https://maps.google.com/?q={Uri.EscapeDataString(tenant.Address)}";
        return base.Redirect(mapsUrl);
    }
}
