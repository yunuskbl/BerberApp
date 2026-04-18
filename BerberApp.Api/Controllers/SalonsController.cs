using BerberApp.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/salons")]
[AllowAnonymous]
public class SalonsController : ControllerBase
{
    private readonly IAppDbContext _context;

    public SalonsController(IAppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? search)
    {
        var query = _context.Tenants
            .Where(x => x.IsActive && !x.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.Name.ToLower().Contains(search.ToLower()) ||
                (x.Address != null && x.Address.ToLower().Contains(search.ToLower())));

        var salons = await query
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Subdomain,
                x.Phone,
                x.Address,
                x.LogoUrl
            })
            .ToListAsync();

        return Ok(new { success = true, data = salons });
    }
}