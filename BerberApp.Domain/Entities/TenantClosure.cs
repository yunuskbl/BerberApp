using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class TenantClosure : BaseEntity
{
    public Guid TenantId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public string? Reason { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
