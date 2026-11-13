using System.Security.Cryptography;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;

namespace BerberApp.Application.Common.Services;

/// <summary>
/// Seans paketlerinin hesap kuralları — işletme ve müşteri uçları aynı
/// kuralları kullansın diye tek yerde.
/// </summary>
public static class SessionPackageRules
{
    public const int MaxSessions       = 200;
    public const decimal MaxPrice      = 10_000_000m;
    public const int MaxNameLength     = 150;
    public const int MaxNoteLength     = 500;
    public const int MaxReasonLength   = 300;
    public const int MaxPaymentNote    = 300;
    public const int MaxValidityDays   = 3650;

    /// <summary>Müşterinin seans onay QR'ını okutması için tanınan süre.</summary>
    public static readonly TimeSpan ConfirmCodeLifetime = TimeSpan.FromMinutes(15);

    /// <summary>
    /// Tahmin edilemez bağlantı anahtarı (24 bayt → 32 karakter, URL-güvenli).
    /// Müşteri takip sayfası ve seans onay kodu giriş istemediği için güvenlik
    /// tamamen bu anahtarın tahmin edilemezliğine dayanıyor.
    /// </summary>
    public static string NewToken()
        => Convert.ToBase64String(RandomNumberGenerator.GetBytes(24))
                  .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static int UsedSessions(IEnumerable<PackageSession> sessions)
        => sessions.Count(s => !s.IsVoid);

    public static decimal Paid(IEnumerable<PackagePayment> payments)
        => payments.Where(p => !p.IsVoid).Sum(p => p.Amount);

    /// <summary>Kalan borç; fazla ödeme kabul edilmediği için eksiye düşmez.</summary>
    public static decimal RemainingDebt(decimal price, decimal paid)
        => Math.Max(0m, Math.Round(price - paid, 2, MidpointRounding.AwayFromZero));

    /// <summary>İptal edilmemiş paketin, kullanılan seansa göre olması gereken durumu.</summary>
    public static CustomerPackageStatus StatusFor(CustomerPackageStatus current, int totalSessions, int usedSessions)
    {
        if (current == CustomerPackageStatus.Cancelled) return CustomerPackageStatus.Cancelled;
        return usedSessions >= totalSessions ? CustomerPackageStatus.Completed : CustomerPackageStatus.Active;
    }

    /// <summary>Süresi geçmiş ama hâlâ kullanılmamış seansı olan paket.</summary>
    public static bool IsExpired(CustomerPackageStatus status, DateTime? expiresAt, DateTime nowUtc)
        => status == CustomerPackageStatus.Active && expiresAt is { } e && e < nowUtc;

    /// <summary>
    /// Seans numaraları: iptal edilmemiş seanslar işlenme sırasına göre 1, 2, 3…
    /// İptal edilen seansın numarası yok; sonraki seanslar boşluk bırakmadan kayar.
    /// </summary>
    public static IReadOnlyList<(PackageSession Session, int? Number)> Numbered(IEnumerable<PackageSession> sessions)
    {
        var n = 0;
        return sessions
            .OrderBy(s => s.PerformedAt).ThenBy(s => s.CreatedAt)
            .Select(s => (s, s.IsVoid ? (int?)null : ++n))
            .ToList();
    }

    /// <summary>"Mehmet Kaya" → "Mehmet K." — müşteriye açık sayfada personelin tam adı yazılmaz.</summary>
    public static string ShortName(string? fullName)
    {
        var parts = (fullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return parts.Length switch
        {
            0 => string.Empty,
            1 => parts[0],
            _ => $"{parts[0]} {char.ToUpper(parts[^1][0])}.",
        };
    }

    /// <summary>"Ayşe Yılmaz" → "Ayşe".</summary>
    public static string FirstName(string? fullName)
        => (fullName ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? string.Empty;
}
