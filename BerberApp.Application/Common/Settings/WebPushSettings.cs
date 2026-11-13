namespace BerberApp.Application.Common.Settings;

/// <summary>
/// Anlık bildirim (Web Push) anahtarları — VAPID.
/// Sunucuda bir kez üretilir (.env → WEBPUSH_PUBLIC_KEY / WEBPUSH_PRIVATE_KEY).
/// Boşsa bildirim özelliği kapalıdır; panel "yapılandırılmamış" der.
/// </summary>
public class WebPushSettings
{
    /// <summary>Açık anahtar, base64url (65 bayt, sıkıştırılmamış P-256 noktası).</summary>
    public string PublicKey { get; set; } = string.Empty;
    /// <summary>Gizli anahtar, base64url (32 bayt).</summary>
    public string PrivateKey { get; set; } = string.Empty;
    /// <summary>Push sunucularının sorun olduğunda ulaşabileceği adres (mailto: ya da https:).</summary>
    public string Subject { get; set; } = "mailto:info@ayarliyo.com";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(PublicKey) && !string.IsNullOrWhiteSpace(PrivateKey);
}
