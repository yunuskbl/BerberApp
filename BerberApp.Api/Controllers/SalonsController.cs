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

        var tenants = await query
            .Select(x => new { x.Id, x.Name, x.Subdomain, x.Address, x.LogoUrl })
            .ToListAsync();

        var tenantIds  = tenants.Select(t => t.Id).ToList();
        var allReviews = await _context.Reviews
            .Where(r => tenantIds.Contains(r.TenantId) && !r.IsDeleted)
            .GroupBy(r => r.TenantId)
            .Select(g => new
            {
                TenantId      = g.Key,
                AverageRating = g.Average(r => r.Rating),
                TotalReviews  = g.Count()
            })
            .ToListAsync();

        var salons = tenants.Select(t =>
        {
            var rv = allReviews.FirstOrDefault(r => r.TenantId == t.Id);
            return new
            {
                t.Id, t.Name, t.Subdomain, t.Address, t.LogoUrl,
                AverageRating = rv != null ? Math.Round(rv.AverageRating, 1) : 0.0,
                TotalReviews  = rv?.TotalReviews ?? 0
            };
        }).ToList();

        return Ok(new { success = true, data = salons });
    }
}