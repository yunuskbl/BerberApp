using BerberApp.Domain.Common;
using BerberApp.Domain.Enums;

namespace BerberApp.Domain.Entities;

/// <summary>
/// Seans paketine yapılan ödeme. Yanlış girilen ödeme silinmez, iptal edilir.
/// </summary>
public class PackagePayment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerPackageId { get; set; }

    public decimal Amount { get; set; }
    public string Currency { get; set; } = "TRY";
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public PackagePaymentMethod Method { get; set; } = PackagePaymentMethod.Cash;
    public string? Note { get; set; }

    public bool IsVoid { get; set; }
    public DateTime? VoidedAt { get; set; }

    // Navigation
    public CustomerPackage CustomerPackage { get; set; } = null!;
}
