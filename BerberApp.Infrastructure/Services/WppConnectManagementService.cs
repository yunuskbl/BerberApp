using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BerberApp.Infrastructure.Services;

public class WppConnectManagementService : IWppConnectManagementService
{
    private readonly HttpClient _http;
    private readonly WppConnectSettings _cfg;
    private readonly ILogger<WppConnectManagementService> _log;

    public WppConnectManagementService(
        HttpClient http,
        IOptions<WppConnectSettings> options,
        ILogger<WppConnectManagementService> logger)
    {
        _http = http;
        _cfg  = options.Value;
        _log  = logger;
    }

    public async Task<string> GenerateTokenAsync(string session, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/generate-token";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.SecretKey);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        _log.LogInformation("[WPPConnect-MGMT] GenerateToken → {Url}", url);
        var res  = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"WPPConnect token oluşturulamadı: {body}");

        using var doc   = JsonDocument.Parse(body);
        var token = doc.RootElement.GetProperty("token").GetString()
            ?? throw new InvalidOperationException("WPPConnect token alanı boş.");
        return token;
    }

    public async Task<WppConnectSessionResult> StartSessionAsync(string session, string token, CancellationToken ct = default)
    {
        // Start session
        var startUrl = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/start-session";
        using var startReq = new HttpRequestMessage(HttpMethod.Post, startUrl);
        startReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        startReq.Content = new StringContent(
            JsonSerializer.Serialize(new { waitForLogin = false }),
            Encoding.UTF8, "application/json");

        var startRes = await _http.SendAsync(startReq, ct);
        _log.LogInformation("[WPPConnect-MGMT] StartSession {Status}", (int)startRes.StatusCode);

        // Get QR code
        var qr = await GetQrCodeAsync(session, token, ct);
        return new WppConnectSessionResult(session, token, qr);
    }

    public async Task<string> GetQrCodeAsync(string session, string token, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/qrcode-session";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res  = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"QR kodu alınamadı: {body}");

        using var doc = JsonDocument.Parse(body);
        var qr = doc.RootElement.TryGetProperty("qrcode", out var qrProp)
            ? qrProp.GetString() ?? ""
            : body;
        return qr;
    }

    public async Task<string> GetStatusAsync(string session, string token, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/status-session";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var res  = await _http.SendAsync(req, ct);
            var body = await res.Content.ReadAsStringAsync(ct);

            using var doc = JsonDocument.Parse(body);
            return doc.RootElement.TryGetProperty("status", out var sp)
                ? sp.GetString() ?? "UNKNOWN"
                : "UNKNOWN";
        }
        catch
        {
            return "DISCONNECTED";
        }
    }

    public async Task CloseSessionAsync(string session, string token, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/close-session";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req, ct);
        _log.LogInformation("[WPPConnect-MGMT] CloseSession {Status}", (int)res.StatusCode);
    }
}
