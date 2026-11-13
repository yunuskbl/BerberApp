using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

/// <summary>
/// Bir kullanıcının bir cihazındaki anlık bildirim (Web Push) aboneliği.
///
/// Tarayıcı aboneliği bir "endpoint" (tarayıcı üreticisinin push sunucusundaki
/// adres) ve iki anahtarla tanımlanır; bildirim bu anahtarlarla şifrelenip o
/// adrese gönderilir. Push sunucusu 404/410 dönerse abonelik geçersizdir ve silinir.
/// </summary>
public class PushSubscription : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid UserId { get; set; }

    public string Endpoint { get; set; } = string.Empty;
    /// <summary>Tarayıcının ECDH açık anahtarı (base64url, 65 bayt).</summary>
    public string P256dh { get; set; } = string.Empty;
    /// <summary>Tarayıcının kimlik doğrulama sırrı (base64url, 16 bayt).</summary>
    public string Auth { get; set; } = string.Empty;

    /// <summary>Abone olunduğunda uygulama ana ekrandan mı açılmıştı (kurulum ölçümü).</summary>
    public bool IsStandalone { get; set; }
    /// <summary>"android", "ios", "desktop" — kabaca, ölçüm için.</summary>
    public string? Platform { get; set; }

    public DateTime? LastSuccessAt { get; set; }
    public int FailureCount { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public User User { get; set; } = null!;
}
