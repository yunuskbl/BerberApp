using BerberApp.Application.Receipt.Dtos;
using MediatR;

namespace BerberApp.Application.Receipt.Queries;

public class GetReceiptByIdQuery : IRequest<ReceiptDto?>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
