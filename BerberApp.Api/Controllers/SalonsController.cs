using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
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

    /// <summary>
    /// Salonları listele. Konum ve sıralama parametrelerini destekler.
    /// sortBy: distance | rating | reviews | newest | name
    /// locationMode (response): nearby | city | all | notFound
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] double? userLat,
        [FromQuery] double? userLon,
        [FromQuery] string sortBy = "rating",
        [FromQuery] BusinessType? businessType = null,
        [FromQuery] double radiusKm = 10.0)
    {
        var query = _context.Tenants
            .Where(x => x.IsActive && !x.IsDeleted && x.IsApproved)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(x =>
                x.Name.ToLower().Contains(search.ToLower()) ||
                (x.City != null && x.City.ToLower().Contains(search.ToLower())) ||
                (x.Address != null && x.Address.ToLower().Contains(search.ToLower())));

        if (businessType.HasValue)
            query = query.Where(x => x.BusinessType == businessType.Value);

        var tenants = await query
            .Select(x => new
            {
                x.Id, x.Name, x.Subdomain, x.Address, x.City,
                x.LogoUrl, x.ThemeColor, x.Latitude, x.Longitude, x.CreatedAt,
                x.BusinessType
            })
            .ToListAsync();

        var tenantIds = tenants.Select(t => t.Id).ToList();

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

        var allPhotos = await _context.TenantPhotos
            .Where(p => tenantIds.Contains(p.TenantId) && !p.IsDeleted)
            .OrderBy(p => p.Order)
            .Select(p => new { p.TenantId, p.Id, p.Url })
            .ToListAsync();

        // Salon nesnelerini oluştur + mesafeyi hesapla
        bool hasUserLocation = userLat.HasValue && userLon.HasValue;

        var salons = tenants.Select(t =>
        {
            var rv     = allReviews.FirstOrDefault(r => r.TenantId == t.Id);
            var photos = allPhotos.Where(p => p.TenantId == t.Id)
                                  .Select(p => new { p.Id, p.Url })
                                  .ToList();

            double? distance = null;
            if (hasUserLocation && t.Latitude.HasValue && t.Longitude.HasValue)
                distance = Haversine(userLat!.Value, userLon!.Value, t.Latitude.Value, t.Longitude.Value);

            return new
            {
                t.Id, t.Name, t.Subdomain, t.Address, t.City,
                t.LogoUrl, t.ThemeColor, t.CreatedAt,
                BusinessType  = t.BusinessType?.ToString(),
                AverageRating = rv != null ? Math.Round(rv.AverageRating, 1) : 0.0,
                TotalReviews  = rv?.TotalReviews ?? 0,
                Photos        = photos,
                Distance      = distance.HasValue ? Math.Round(distance.Value, 1) : (double?)null
            };
        }).ToList();

        // ─── Konum bazlı filtreleme ─────────────────────────────────────────
        string locationMode = "all";

        if (hasUserLocation && string.IsNullOrWhiteSpace(search))
        {
            var withCoords    = salons.Where(s => s.Distance.HasValue).ToList();
            var withoutCoords = salons.Where(s => !s.Distance.HasValue).ToList();
            var inRadius      = withCoords.Where(s => s.Distance!.Value <= radiusKm).ToList();

            if (inRadius.Count > 0)
            {
                salons       = [..inRadius, ..withoutCoords];
                locationMode = "nearby";
            }
            else
            {
                locationMode = "notFound";
                salons       = [];
            }
        }

        // ─── Sıralama ───────────────────────────────────────────────────────
        salons = sortBy switch
        {
            "distance" when hasUserLocation =>
                salons.OrderBy(s => s.Distance ?? double.MaxValue)
                      .ThenByDescending(s => s.AverageRating).ToList(),
            "reviews"  => salons.OrderByDescending(s => s.TotalReviews)
                                .ThenByDescending(s => s.AverageRating).ToList(),
            "newest"   => salons.OrderByDescending(s => s.CreatedAt).ToList(),
            "name"     => salons.OrderBy(s => s.Name).ToList(),
            _          => // "rating" — varsayılan
                salons.OrderByDescending(s => s.AverageRating)
                      .ThenByDescending(s => s.TotalReviews).ToList(),
        };

        return Ok(new
        {
            success      = true,
            data         = salons,
            locationMode,              // nearby | city | all | notFound
            totalCount   = salons.Count
        });
    }

    // ─── Haversine mesafe formülü (km) ─────────────────────────────────────
    private static double Haversine(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371.0;
        var dLat = (lat2 - lat1) * Math.PI / 180.0;
        var dLon = (lon2 - lon1) * Math.PI / 180.0;
        var a    = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1 * Math.PI / 180.0) * Math.Cos(lat2 * Math.PI / 180.0) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }
}
