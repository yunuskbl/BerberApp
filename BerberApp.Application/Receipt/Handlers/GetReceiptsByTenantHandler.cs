using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Receipt.Dtos;
using BerberApp.Application.Receipt.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Receipt.Handlers;

public class GetReceiptsByTenantHandler : IRequestHandler<GetReceiptsByTenantQuery, List<ReceiptDto>>
{
    private readonly IAppDbContext _context;

    public GetReceiptsByTenantHandler(IAppDbContext context) => _context = context;

    public async Task<List<ReceiptDto>> Handle(GetReceiptsByTenantQuery request, CancellationToken ct)
    {
        var query = _context.Receipts
            .Include(r => r.Items)
            .Include(r => r.Customer)
            .Where(r => r.TenantId == request.TenantId);

        if (request.From.HasValue)
            query = query.Where(r => r.CreatedAt >= request.From.Value);
        if (request.To.HasValue)
            query = query.Where(r => r.CreatedAt <= request.To.Value);
        if (request.CustomerId.HasValue)
            query = query.Where(r => r.CustomerId == request.CustomerId.Value);

        var receipts = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);

        return receipts.Select(r => CreateReceiptHandler.ToDto(r, r.Customer?.FullName)).ToList();
    }
}
