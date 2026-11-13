namespace BerberApp.Application.Common.Interfaces;

/// <summary>Panelde tıklanınca açılacak sayfayla birlikte bir anlık bildirim.</summary>
/// <param name="Tag">Aynı etiketli yeni bildirim eskisinin yerine geçer (yığılmasın).</param>
public sealed record PushMessage(string Title, string Body, string Url, string? Tag = null);

public interface IPushNotificationService
{
    bool IsEnabled { get; }

    /// <summary>
    /// İşletmenin yöneticilerine ve (verilmişse) ilgili personelin kendisine gönderir.
    /// Geçersizleşmiş abonelikleri siler. Gönderilen cihaz sayısını döner.
    /// </summary>
    Task<int> SendToTenantAsync(Guid tenantId, PushMessage message, Guid? staffId = null, CancellationToken ct = default);

    /// <summary>Tek bir kullanıcının tüm cihazlarına (deneme bildirimi).</summary>
    Task<int> SendToUserAsync(Guid userId, PushMessage message, CancellationToken ct = default);
}
