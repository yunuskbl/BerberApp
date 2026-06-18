using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Receipt.Commands;
using BerberApp.Application.Receipt.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Receipt.Handlers;

public class CreateReceiptHandler : IRequestHandler<CreateReceiptCommand, ReceiptDto>
{
    private readonly IAppDbContext _context;

    public CreateReceiptHandler(IAppDbContext context) => _context = context;

    public async Task<ReceiptDto> Handle(CreateReceiptCommand request, CancellationToken ct)
    {
        var year = DateTime.UtcNow.Year;
        var lastNumber = await _context.Receipts
            .Where(r => r.TenantId == request.TenantId && r.ReceiptNumber.StartsWith(year + "-"))
            .OrderByDescending(r => r.ReceiptNumber)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(ct);

        int seq = 1;
        if (lastNumber != null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int last))
                seq = last + 1;
        }

        var receiptNumber = $"{year}-{seq:D4}";

        var items = request.Items.Select(i => new ReceiptItemEntity
        {
            ServiceName = i.ServiceName,
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice,
        }).ToList();

        var receipt = new ReceiptEntity
        {
            TenantId      = request.TenantId,
            CustomerId    = request.CustomerId,
            AppointmentId = request.AppointmentId,
            ReceiptNumber = receiptNumber,
            TotalAmount   = items.Sum(i => i.Quantity * i.UnitPrice),
            Note          = request.Note,
            Items         = items,
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync(ct);

        string? customerName = null;
        if (request.CustomerId.HasValue)
        {
            customerName = await _context.Customers
                .Where(c => c.Id == request.CustomerId.Value)
                .Select(c => c.FullName)
                .FirstOrDefaultAsync(ct);
        }

        return ToDto(receipt, customerName);
    }

    internal static ReceiptDto ToDto(ReceiptEntity r, string? customerName) => new()
    {
        Id            = r.Id,
        ReceiptNumber = r.ReceiptNumber,
        CustomerName  = customerName,
        CustomerId    = r.CustomerId,
        AppointmentId = r.AppointmentId,
        TotalAmount   = r.TotalAmount,
        Currency      = r.Currency,
        Note          = r.Note,
        IsVoid        = r.IsVoid,
        CreatedAt     = r.CreatedAt,
        Items         = r.Items.Select(i => new ReceiptItemDto
        {
            Id          = i.Id,
            ServiceName = i.ServiceName,
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice,
        }).ToList(),
    };
}
