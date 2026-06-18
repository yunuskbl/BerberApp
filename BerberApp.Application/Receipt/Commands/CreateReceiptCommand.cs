using BerberApp.Application.Receipt.Dtos;
using MediatR;

namespace BerberApp.Application.Receipt.Commands;

public class CreateReceiptCommand : IRequest<ReceiptDto>
{
    public Guid TenantId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string? Note { get; set; }
    public List<CreateReceiptItemDto> Items { get; set; } = new();
}

public class CreateReceiptItemDto
{
    public string ServiceName { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
}
