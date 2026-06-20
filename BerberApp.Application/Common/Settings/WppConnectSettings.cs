namespace BerberApp.Application.Common.Settings;

public class WppConnectSettings
{
    /// <summary>WPPConnect sunucu adresi — örn. http://wppconnect:21465</summary>
    public string BaseUrl { get; set; } = "http://localhost:21465";

    /// <summary>generate-token endpoint'inde kullanılan gizli anahtar</summary>
    public string SecretKey { get; set; } = "";

    /// <summary>WPPConnect oturum adı</summary>
    public string Session { get; set; } = "ayarliyo";

    /// <summary>generate-token çağrısından dönen bearer token (bir kez alınır, saklanır)</summary>
    public string Token { get; set; } = "";
}
