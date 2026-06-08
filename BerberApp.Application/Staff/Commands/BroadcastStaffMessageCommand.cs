using MediatR;

namespace BerberApp.Application.Staff.Commands;

public class BroadcastStaffMessageCommand : IRequest<BroadcastStaffMessageResult>
{
    public Guid TenantId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BroadcastStaffMessageResult
{
    public int TotalStaff { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
}
