using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Receipt.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Receipt.Handlers;

public class VoidReceiptHandler : IRequestHandler<VoidReceiptCommand, bool>
{
    private readonly IAppDbContext _context;

    public VoidReceiptHandler(IAppDbContext context) => _context = context;

    public async Task<bool> Handle(VoidReceiptCommand request, CancellationToken ct)
    {
        var receipt = await _context.Receipts
            .FirstOrDefaultAsync(r => r.Id == request.Id && r.TenantId == request.TenantId, ct);

        if (receipt is null) return false;

        receipt.IsVoid = true;
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
