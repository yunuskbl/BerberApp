using MediatR;

namespace BerberApp.Application.Staff.Commands;

public class BroadcastStaffMessageCommand : IRequest<BroadcastStaffMessageResult>
{
    public Guid TenantId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Gönderim arka planda yürür; bu sonuç işin kuyruğa alındığını bildirir.
/// İlerleme BroadcastJobId ile sorgulanır.
/// </summary>
public class BroadcastStaffMessageResult
{
    public Guid BroadcastJobId { get; set; }
    public int TotalStaff { get; set; }
}
