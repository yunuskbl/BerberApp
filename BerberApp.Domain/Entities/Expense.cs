using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class Expense : BaseEntity
{
    public Guid TenantId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public string Category { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Note { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
}
