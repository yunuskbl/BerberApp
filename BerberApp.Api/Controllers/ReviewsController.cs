using BerberApp.Api.Authorization;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

/// <summary>Admin paneli — tenant'ın kendi yorumlarını görür.</summary>
[RequirePlan(PlanType.Profesyonel, PlanType.Premium)]
public class ReviewsController : BaseApiController
{
    private readonly IAppDbContext _context;

    public ReviewsController(IMediator mediator, IAppDbContext context) : base(mediator)
        => _context = context;

    [HttpGet]
    public async Task<IActionResult> GetReviews()
    {
        var reviews = await _context.Reviews
            .Where(r => r.TenantId == TenantId && !r.IsDeleted)
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new
            {
                r.Id,
                r.Rating,
                r.CustomerName,
                r.CustomerId,
                r.Comment,
                r.CreatedAt,
                CustomerPhone = _context.Customers
                    .Where(c => c.Id == r.CustomerId)
                    .Select(c => c.Phone)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // Birden fazla yorum yapan müşterileri işaretle
        var repeatCustomerIds = reviews
            .Where(r => r.CustomerId.HasValue)
            .GroupBy(r => r.CustomerId)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        var enriched = reviews.Select(r => new
        {
            r.Id,
            r.Rating,
            r.CustomerName,
            r.CustomerId,
            r.CustomerPhone,
            r.Comment,
            r.CreatedAt,
            IsRepeatCustomer = r.CustomerId.HasValue && repeatCustomerIds.Contains(r.CustomerId)
        }).ToList();

        var total   = reviews.Count;
        var average = total > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0;
        var dist    = Enumerable.Range(1, 5)
            .Select(s => new { star = s, count = reviews.Count(r => r.Rating == s) })
            .ToList();

        return Success(new { total, average, distribution = dist, reviews = enriched });
    }
}
