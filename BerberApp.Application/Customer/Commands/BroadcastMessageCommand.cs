using MediatR;

namespace BerberApp.Application.Customer.Commands;

public enum BroadcastFilter
{
    All,              // Tüm müşteriler
    NotVisitedSince,  // Son X gün gelmeyenler
    NeverVisited,     // Hiç gelmeyenler
    FrequentCustomers,// En az X randevusu olanlar
    RecentVisitors    // Son X günde gelenler
}

public class BroadcastMessageCommand : IRequest<BroadcastMessageResult>
{
    public Guid TenantId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public BroadcastFilter Filter { get; set; } = BroadcastFilter.All;
    public int? FilterDays { get; set; }
    public int? MinAppointments { get; set; }
}

/// <summary>
/// Gönderim artık arka planda yürüdüğü için bu sonuç yalnızca işin kuyruğa
/// alındığını bildirir. Gerçek ilerleme BroadcastJobId ile sorgulanır.
/// </summary>
public class BroadcastMessageResult
{
    public Guid BroadcastJobId { get; set; }
    public int TotalCustomers { get; set; }
    public int FilteredCount { get; set; }
}
