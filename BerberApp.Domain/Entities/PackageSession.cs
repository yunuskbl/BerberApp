using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

/// <summary>
/// Paketten kullanılan bir seans.
///
/// Seansı personel işler; seans o anda düşer. Müşteri salonda gösterilen QR'ı
/// okutursa <see cref="CustomerConfirmedAt"/> dolar ve seans iki tarafta da
/// "müşteri onayladı" olarak görünür. Yanlış işlenen seans silinmez, iptal
/// edilir (<see cref="IsVoid"/>) — geçmiş iki taraf için de izlenebilir kalır.
/// </summary>
public class PackageSession : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerPackageId { get; set; }
    public Guid? StaffId { get; set; }

    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? Note { get; set; }

    /// <summary>Müşterinin okutacağı tek kullanımlık onay kodu (QR'ın içeriği).</summary>
    public string? ConfirmCode { get; set; }
    public DateTime? ConfirmCodeExpiresAt { get; set; }
    public DateTime? CustomerConfirmedAt { get; set; }

    public bool IsVoid { get; set; }
    public DateTime? VoidedAt { get; set; }
    public string? VoidReason { get; set; }

    // Navigation
    public CustomerPackage CustomerPackage { get; set; } = null!;
    public Staff? Staff { get; set; }
}
