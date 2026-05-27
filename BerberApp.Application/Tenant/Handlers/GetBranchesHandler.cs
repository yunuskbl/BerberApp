using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Tenant.Handlers;

public class GetBranchesHandler : IRequestHandler<GetBranchesQuery, List<BranchDto>>
{
    private readonly IAppDbContext _context;

    public GetBranchesHandler(IAppDbContext context) => _context = context;

    public async Task<List<BranchDto>> Handle(GetBranchesQuery request, CancellationToken ct)
    {
        return await _context.Tenants
            .Where(t => t.ParentTenantId == request.ParentTenantId && !t.IsDeleted)
            .OrderBy(t => t.CreatedAt)
            .Select(t => new BranchDto
            {
                Id        = t.Id,
                Name      = t.Name,
                Subdomain = t.Subdomain,
                Phone     = t.Phone,
                Address   = t.Address,
                City      = t.City,
                IsActive  = t.IsActive,
                CreatedAt = t.CreatedAt,
            })
            .ToListAsync(ct);
    }
}
