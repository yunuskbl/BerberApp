namespace BerberApp.Application.Common.Interfaces;

public interface IWppConnectManagementService
{
    Task<WppConnectSessionResult> StartSessionAsync(string session, string token, CancellationToken ct = default);
    Task<string> GenerateTokenAsync(string session, CancellationToken ct = default);
    Task<string> GetQrCodeAsync(string session, string token, CancellationToken ct = default);

    /// <summary>
    /// Oturum durumunu, "ulaşılamadı" ile "kopuk" ayrımını koruyarak döndürür.
    /// </summary>
    Task<WppConnectStatus> GetSessionStatusAsync(string session, string token, CancellationToken ct = default);

    /// <summary>
    /// Yalnızca durum metnini döndüren eski imza. Yeni kodda
    /// <see cref="GetSessionStatusAsync"/> tercih edilmeli — bu metot
    /// "ulaşılamadı" bilgisini kaybeder.
    /// </summary>
    Task<string> GetStatusAsync(string session, string token, CancellationToken ct = default);
    Task CloseSessionAsync(string session, string token, CancellationToken ct = default);
    Task LogoutSessionAsync(string session, string token, CancellationToken ct = default);
    Task DeleteSessionAsync(string session, string token, CancellationToken ct = default);
    Task<string> RequestPairingCodeAsync(string session, string token, string phoneNumber, CancellationToken ct = default);
}

public record WppConnectSessionResult(string Session, string Token, string QrCode, bool IsConnected = false);

/// <summary>
/// WPPConnect oturum durumu.
///
/// <paramref name="IsReachable"/> ayrımı kritik: WPPConnect'e hiç ulaşılamadığında
/// (timeout, HTTP hatası, servis kapalı) bu "oturum kopuk" demek değil, "durumu
/// bilmiyoruz" demektir. Eskiden her iki durum da "DISCONNECTED" olarak
/// raporlandığı için, WPPConnect bir saniye yavaşladığında sağlıklı hatlar
/// kopuk sayılıp toplu mesaj hiç başlatılmıyordu.
/// </summary>
public record WppConnectStatus(string Value, bool IsReachable)
{
    private static readonly string[] ConnectedValues = ["CONNECTED", "inChat", "isLogged"];

    /// <summary>Oturumun gönderime hazır olduğu kesin olarak biliniyor mu?</summary>
    public bool IsConnected =>
        IsReachable && ConnectedValues.Contains(Value, StringComparer.OrdinalIgnoreCase);

    /// <summary>Oturumun gönderime hazır OLMADIĞI kesin olarak biliniyor mu?</summary>
    public bool IsDefinitelyDisconnected => IsReachable && !IsConnected;
}
