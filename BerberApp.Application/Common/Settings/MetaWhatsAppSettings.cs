namespace BerberApp.Application.Common.Settings;

public class MetaWhatsAppSettings
{
    /// <summary>Meta Business Manager'dan alınan WhatsApp telefon numarası ID'si</summary>
    public string PhoneNumberId { get; set; } = string.Empty;

    /// <summary>Meta System User veya Page Access Token</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>Graph API sürümü (ör. v21.0)</summary>
    public string ApiVersion { get; set; } = "v21.0";

    /// <summary>Şablon dil kodu</summary>
    public string TemplateLanguage { get; set; } = "tr";

    /// <summary>Meta webhook doğrulama token'ı (Meta Developer Console'da ayarlanan)</summary>
    public string WebhookVerifyToken { get; set; } = string.Empty;

    /// <summary>Meta App Secret — gelen webhook'ların X-Hub-Signature-256 HMAC doğrulaması için</summary>
    public string AppSecret { get; set; } = string.Empty;

    /// <summary>Onaylı Meta şablon adları (anahtar: kod adı, değer: Meta'daki şablon adı)</summary>
    public Dictionary<string, string> Templates { get; set; } = new();
}
