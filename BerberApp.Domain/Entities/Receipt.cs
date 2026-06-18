using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class Receipt : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? CustomerId { get; set; }
    public Guid? AppointmentId { get; set; }
    public string ReceiptNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string? Note { get; set; }
    public bool IsVoid { get; set; } = false;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Customer? Customer { get; set; }
    public Appointment? Appointment { get; set; }
    public ICollection<ReceiptItem> Items { get; set; } = new List<ReceiptItem>();
}
