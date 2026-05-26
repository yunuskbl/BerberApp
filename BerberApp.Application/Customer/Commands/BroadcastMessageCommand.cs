using MediatR;

namespace BerberApp.Application.Customer.Commands;

public class BroadcastMessageCommand : IRequest<BroadcastMessageResult>
{
    public Guid TenantId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BroadcastMessageResult
{
    public int TotalCustomers { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
}
