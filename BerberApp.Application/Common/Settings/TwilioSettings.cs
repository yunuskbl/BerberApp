namespace BerberApp.Application.Common.Settings;

public class TwilioSettings
{
    public string AccountSid   { get; set; } = string.Empty;
    public string AuthToken    { get; set; } = string.Empty;
    /// <summary>Sandbox veya fallback numarası (serbest metin mesajları için)</summary>
    public string FromNumber   { get; set; } = string.Empty;
    /// <summary>Content Template API mesajları için onaylı WhatsApp iş numarası</summary>
    public string WhatsAppFrom { get; set; } = string.Empty;
    /// <summary>Twilio Content Template SID'leri (anahtar: şablon adı)</summary>
    public Dictionary<string, string> Templates { get; set; } = new();
}
