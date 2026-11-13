namespace BerberApp.Domain.Enums;

/// <summary>Müşteriye satılan seans paketinin durumu.</summary>
public enum CustomerPackageStatus
{
    /// <summary>Kullanılmamış seansı var.</summary>
    Active = 0,
    /// <summary>Tüm seanslar kullanıldı.</summary>
    Completed = 1,
    /// <summary>İşletme tarafından iptal edildi.</summary>
    Cancelled = 2,
}

/// <summary>Pakete yapılan ödemenin şekli.</summary>
public enum PackagePaymentMethod
{
    Cash = 0,
    Card = 1,
    Transfer = 2,
    Other = 3,
}
