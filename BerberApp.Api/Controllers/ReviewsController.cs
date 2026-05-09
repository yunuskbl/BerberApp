using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

/// <summary>Admin paneli — tenant'ın kendi yorumlarını görür.</summary>
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
                r.Comment,
                r.CreatedAt
            })
            .ToListAsync();

        var total   = reviews.Count;
        var average = total > 0 ? Math.Round(reviews.Average(r => r.Rating), 1) : 0.0;
        var dist    = Enumerable.Range(1, 5)
            .Select(s => new { star = s, count = reviews.Count(r => r.Rating == s) })
            .ToList();

        return Success(new { total, average, distribution = dist, reviews });
    }
}
