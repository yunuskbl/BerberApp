using BerberApp.Domain.Common;
using BerberApp.Domain.Enums;

namespace BerberApp.Domain.Entities;

public class PaymentTransaction : BaseEntity
{
    public Guid   TenantId    { get; set; }
    public string Plan        { get; set; } = "";
    public decimal Amount     { get; set; }
    public string Currency    { get; set; } = "TRY";
    public PaymentTransactionStatus Status { get; set; }

    // Iyzico referans kimlikleri
    public string? ConversationId            { get; set; }
    public string? IyzicoPaymentId           { get; set; }
    public string? IyzicoPaymentTransactionId { get; set; }

    public string? ErrorMessage  { get; set; }
    public DateTime? RefundedAt  { get; set; }

    public virtual Tenant Tenant { get; set; } = null!;
}
