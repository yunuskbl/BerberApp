using BerberApp.Domain.Common;
using BerberApp.Domain.Enums;

namespace BerberApp.Domain.Entities;

/// <summary>
/// Müşteriye satılmış seans paketi.
///
/// Ad, seans sayısı ve fiyat satış anında kopyalanır; şablon sonradan
/// değişse de müşteriyle yapılan anlaşma değişmez. Müşteri paketini
/// <see cref="PublicToken"/> taşıyan bağlantıdan (QR) giriş yapmadan izler.
/// </summary>
public class CustomerPackage : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid? PackageTemplateId { get; set; }
    public Guid? ServiceId { get; set; }

    public string Name { get; set; } = string.Empty;
    public int TotalSessions { get; set; }
    /// <summary>Anlaşılan toplam ücret; kalan borç = Price − geçerli ödemeler.</summary>
    public decimal Price { get; set; }
    public string Currency { get; set; } = "TRY";

    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    /// <summary>Son kullanma; null = süresiz.</summary>
    public DateTime? ExpiresAt { get; set; }

    public CustomerPackageStatus Status { get; set; } = CustomerPackageStatus.Active;
    public DateTime? CancelledAt { get; set; }
    public string? CancelReason { get; set; }

    /// <summary>Müşteri takip sayfasının tahmin edilemez anahtarı.</summary>
    public string PublicToken { get; set; } = string.Empty;
    public string? Note { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public PackageTemplate? PackageTemplate { get; set; }
    public Service? Service { get; set; }
    public ICollection<PackageSession> Sessions { get; set; } = new List<PackageSession>();
    public ICollection<PackagePayment> Payments { get; set; } = new List<PackagePayment>();
}
