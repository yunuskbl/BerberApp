using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

/// <summary>
/// İşletmenin tanımladığı seans paketi, ör. "Lazer Epilasyon — 8 seans".
/// Müşteriye satılırken değerleri <see cref="CustomerPackage"/>'a kopyalanır;
/// şablon sonradan değişse de satılmış paket etkilenmez.
/// </summary>
public class PackageTemplate : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid? ServiceId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SessionCount { get; set; }
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";
    /// <summary>Satıştan itibaren kaç gün geçerli; null = süresiz.</summary>
    public int? ValidityDays { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Service? Service { get; set; }
}
