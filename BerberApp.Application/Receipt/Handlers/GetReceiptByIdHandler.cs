using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Receipt.Dtos;
using BerberApp.Application.Receipt.Queries;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Receipt.Handlers;

public class GetReceiptByIdHandler : IRequestHandler<GetReceiptByIdQuery, ReceiptDto?>
{
    private readonly IAppDbContext _context;

    public GetReceiptByIdHandler(IAppDbContext context) => _context = context;

    public async Task<ReceiptDto?> Handle(GetReceiptByIdQuery request, CancellationToken ct)
    {
        var receipt = await _context.Receipts
            .Include(r => r.Items)
            .Include(r => r.Customer)
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct);

        return receipt is null ? null : CreateReceiptHandler.ToDto(receipt, receipt.Customer?.FullName);
    }
}
