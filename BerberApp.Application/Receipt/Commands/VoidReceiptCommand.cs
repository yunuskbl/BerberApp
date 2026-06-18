using MediatR;

namespace BerberApp.Application.Receipt.Commands;

public class VoidReceiptCommand : IRequest<bool>
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
}
