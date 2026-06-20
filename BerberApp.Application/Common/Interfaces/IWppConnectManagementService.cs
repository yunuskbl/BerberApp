namespace BerberApp.Application.Common.Interfaces;

public interface IWppConnectManagementService
{
    Task<WppConnectSessionResult> StartSessionAsync(string session, string token, CancellationToken ct = default);
    Task<string> GenerateTokenAsync(string session, CancellationToken ct = default);
    Task<string> GetQrCodeAsync(string session, string token, CancellationToken ct = default);
    Task<string> GetStatusAsync(string session, string token, CancellationToken ct = default);
    Task CloseSessionAsync(string session, string token, CancellationToken ct = default);
}

public record WppConnectSessionResult(string Session, string Token, string QrCode);
