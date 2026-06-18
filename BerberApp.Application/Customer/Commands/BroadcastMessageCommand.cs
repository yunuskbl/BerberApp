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
    public BroadcastFilter Filter { get; set; } = BroadcastFilter.All;
    public int? FilterDays { get; set; }
    public int? MinAppointments { get; set; }
}

public class BroadcastMessageResult
{
    public int TotalCustomers { get; set; }
    public int FilteredCount { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
}
