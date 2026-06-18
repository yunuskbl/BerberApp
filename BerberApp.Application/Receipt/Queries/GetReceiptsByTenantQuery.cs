using BerberApp.Application.Receipt.Dtos;
using MediatR;

namespace BerberApp.Application.Receipt.Queries;

public class GetReceiptsByTenantQuery : IRequest<List<ReceiptDto>>
{
    public Guid TenantId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public Guid? CustomerId { get; set; }
}
