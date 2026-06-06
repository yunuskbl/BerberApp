using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class PriceDifference : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid AppointmentId { get; set; }
    public decimal OriginalPrice { get; set; }
    public decimal ActualPrice { get; set; }
    public decimal Difference { get; set; }
    public DateTime CompletedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
    public Appointment Appointment { get; set; } = null!;
}
