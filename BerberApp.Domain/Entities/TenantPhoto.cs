using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class TenantPhoto : BaseEntity
{
    public Guid TenantId { get; set; }
    public string Url { get; set; } = string.Empty;
    public int Order { get; set; } = 0;

    public Tenant Tenant { get; set; } = null!;
}
