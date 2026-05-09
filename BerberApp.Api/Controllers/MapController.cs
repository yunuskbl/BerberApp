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

        if (tenant is null)
            return NotFound("Salon bulunamadı.");

        // Adres varsa adresle, yoksa salon adıyla Google Maps'te ara
        var query   = !string.IsNullOrWhiteSpace(tenant.Address) ? tenant.Address : tenant.Name;
        var mapsUrl = $"https://maps.google.com/?q={Uri.EscapeDataString(query)}";
        return base.Redirect(mapsUrl);
    }
}
