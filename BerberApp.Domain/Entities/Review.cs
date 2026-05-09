using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class Review : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? AppointmentId { get; set; }
    public Guid? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public int Rating { get; set; }          // 1–5
    public string? Comment { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Appointment? Appointment { get; set; }
}
