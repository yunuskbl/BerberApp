using BerberApp.Domain.Common;
using BerberApp.Domain.Enums;

namespace BerberApp.Domain.Entities;

public class PaymentRequest : BaseEntity
{
    public Guid TenantId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public string PlanLabel { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string ReferenceCode { get; set; } = string.Empty;
    public PaymentRequestStatus Status { get; set; } = PaymentRequestStatus.Pending;
    public string? AdminNotes { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Tenant Tenant { get; set; } = null!;
}
