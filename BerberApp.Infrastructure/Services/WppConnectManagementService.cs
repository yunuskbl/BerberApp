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
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/{_cfg.SecretKey}/generate-token";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        // Güvenlik: URL SecretKey içeriyor — maskeleyerek logla
        _log.LogInformation("[WPPConnect-MGMT] GenerateToken → {Url}",
            $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/***/generate-token");
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
        var base_ = _cfg.BaseUrl.TrimEnd('/');

        // Varsa eski session'ı kapat (CLOSED state'ten temiz başlayalım)
        try
        {
            var closeReq = new HttpRequestMessage(HttpMethod.Post, $"{base_}/api/{session}/close-session");
            closeReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            closeReq.Content = new StringContent("{}", Encoding.UTF8, "application/json");
            await _http.SendAsync(closeReq, ct);
            await Task.Delay(1000, ct);
        }
        catch { /* görmezden gel */ }

        // WppConnect 2.x: start-session is async, QR is generated in the background.
        // waitQrCode is ignored in v2.10+, so we just start and then poll.
        var startUrl = $"{base_}/api/{session}/start-session";
        using var startReq = new HttpRequestMessage(HttpMethod.Post, startUrl);
        startReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        startReq.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var startRes = await _http.SendAsync(startReq, ct);
        var startBody = await startRes.Content.ReadAsStringAsync(ct);
        _log.LogInformation("[WPPConnect-MGMT] StartSession {Status}: {Body}", (int)startRes.StatusCode, startBody);

        // Try to extract QR from start-session response first
        if (TryExtractQr(startBody, out var qrFromStart) && !string.IsNullOrWhiteSpace(qrFromStart))
            return new WppConnectSessionResult(session, token, qrFromStart);

        // Poll for QR — WppConnect 2.x generates it asynchronously (up to 40 seconds)
        string qr = "";
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(2000, ct);
            try
            {
                qr = await GetQrCodeAsync(session, token, ct);
                _log.LogInformation("[WPPConnect-MGMT] QR poll {Attempt}/20: {Result}",
                    i + 1, string.IsNullOrWhiteSpace(qr) ? "empty" : qr == "ALREADY_CONNECTED" ? "already connected" : "got QR");

                if (qr == "ALREADY_CONNECTED")
                    return new WppConnectSessionResult(session, token, "", IsConnected: true);

                if (!string.IsNullOrWhiteSpace(qr)) break;
            }
            catch (Exception ex)
            {
                _log.LogWarning("[WPPConnect-MGMT] QR poll {Attempt}/20 error: {Msg}", i + 1, ex.Message);
            }
        }

        return new WppConnectSessionResult(session, token, qr);
    }

    private static bool TryExtractQr(string body, out string qr)
    {
        qr = string.Empty;
        if (string.IsNullOrWhiteSpace(body) || body.TrimStart().StartsWith('<')) return false;
        try
        {
            using var doc = JsonDocument.Parse(body);
            foreach (var field in new[] { "qrcode", "base64Qr", "qr", "urlCode" })
            {
                if (doc.RootElement.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    qr = v.GetString() ?? string.Empty;
                    if (!string.IsNullOrWhiteSpace(qr)) return true;
                }
            }
        }
        catch { /* geçersiz JSON */ }
        return false;
    }

    public async Task<string> GetQrCodeAsync(string session, string token, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/qrcode-session";
        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var res = await _http.SendAsync(req, ct);

        // PNG image stream olarak dönebilir
        var contentType = res.Content.Headers.ContentType?.MediaType ?? "";
        if (contentType.Contains("image"))
        {
            var bytes = await res.Content.ReadAsByteArrayAsync(ct);
            _log.LogInformation("[WPPConnect-MGMT] GetQr: image/{Type} {Bytes}B", contentType, bytes.Length);
            return $"data:{contentType};base64,{Convert.ToBase64String(bytes)}";
        }

        var body = await res.Content.ReadAsStringAsync(ct);
        _log.LogInformation("[WPPConnect-MGMT] GetQr {Status} ct={ContentType}: {Body}",
            (int)res.StatusCode, contentType, body.Length > 200 ? body[..200] : body);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"QR kodu alınamadı: {body}");

        // JSON yanıtı: farklı WppConnect versiyonları farklı alan adı kullanır
        try
        {
            using var doc = JsonDocument.Parse(body);

            // Session auto-connected (had saved credentials) — signal the caller
            if (doc.RootElement.TryGetProperty("status", out var statusProp))
            {
                var status = statusProp.GetString() ?? "";
                if (status is "CONNECTED" or "inChat" or "isLogged")
                    return "ALREADY_CONNECTED";
            }

            foreach (var field in new[] { "qrcode", "base64Qr", "qr", "urlCode" })
            {
                if (doc.RootElement.TryGetProperty(field, out var qrProp))
                {
                    var qr = qrProp.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(qr)) return qr;
                }
            }
        }
        catch { /* JSON değil */ }

        // Geçerli bir QR verisi değilse boş döndür
        return string.Empty;
    }

    /// <summary>
    /// Durum sorgusu için üst sınır. HttpClient'ın genel timeout'u oturum
    /// başlatma (içinde 20×2s QR polling var) için uzun tutulmak zorunda;
    /// durum sorgusunun o kadar beklemesi anlamsız, kullanıcıyı bekletir.
    /// </summary>
    private static readonly TimeSpan StatusTimeout = TimeSpan.FromSeconds(10);

    public async Task<WppConnectStatus> GetSessionStatusAsync(string session, string token, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/status-session";

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(StatusTimeout);

        using var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var res  = await _http.SendAsync(req, timeout.Token);
            var body = await res.Content.ReadAsStringAsync(timeout.Token);

            // HTTP hatası WPPConnect'in kendisinden geliyor olabilir de, araya
            // giren bir şeyden de — ikisi de "oturum kopuk" kanıtı değil.
            if (!res.IsSuccessStatusCode)
            {
                _log.LogWarning("[WPPConnect] Durum sorgusu HTTP {Status} ({Session})", (int)res.StatusCode, session);
                return new WppConnectStatus($"HTTP_{(int)res.StatusCode}", IsReachable: false);
            }

            using var doc = JsonDocument.Parse(body);
            var value = doc.RootElement.TryGetProperty("status", out var sp)
                ? sp.GetString() ?? "UNKNOWN"
                : "UNKNOWN";

            return new WppConnectStatus(value, IsReachable: true);
        }
        catch (Exception ex)
        {
            _log.LogWarning(ex, "[WPPConnect] Oturum durumu okunamadı ({Session})", session);
            return new WppConnectStatus("UNREACHABLE", IsReachable: false);
        }
    }

    public async Task<string> GetStatusAsync(string session, string token, CancellationToken ct = default)
        => (await GetSessionStatusAsync(session, token, ct)).Value;

    /// <summary>
    /// Oturumu WhatsApp tarafından da düşürerek tamamen serbest bırakır.
    ///
    /// close-session yalnızca sunucudaki tarayıcıyı kapatır; telefondaki
    /// "Bağlı Cihazlar" listesinde kayıt durmaya devam eder. Cihazı gerçekten
    /// listeden düşüren çağrı logout-session. Yıllardır yalnızca close
    /// çağrıldığı için her yeniden bağlanma telefonda bir hayalet kayıt
    /// bırakıyor, WhatsApp'ın 4 cihazlık sınırı dolunca da yeni bağlantı
    /// "Şu anda yeni cihaz bağlanamıyor" hatasıyla reddediliyordu.
    ///
    /// Üç adımın hepsi best-effort: oturum zaten ölüyse logout başarısız olur,
    /// bu durumda bile delete ile sunucudaki token klasörünü temizlemek
    /// gerekiyor. Hiçbir hata çağıranı durdurmamalı — bu bir temizlik işlemi.
    /// </summary>
    public async Task ReleaseSessionAsync(string session, string token, CancellationToken ct = default)
    {
        // 1) WhatsApp'a cihazın bağlantısını kes dedir — asıl önemli adım.
        try { await LogoutSessionAsync(session, token, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "[WPPConnect-MGMT] Logout başarısız: {Session}", session); }

        // 2) Tarayıcıyı kapat (logout zaten kapatmış olabilir, zararsız).
        try { await CloseSessionAsync(session, token, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "[WPPConnect-MGMT] Close başarısız: {Session}", session); }

        // 3) Sunucudaki oturum dosyalarını sil — her bağlanmada yeni bir oturum
        //    adı üretildiği için temizlenmezse token klasörleri birikir.
        try { await DeleteSessionAsync(session, token, ct); }
        catch (Exception ex) { _log.LogWarning(ex, "[WPPConnect-MGMT] Delete başarısız: {Session}", session); }
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

    public async Task LogoutSessionAsync(string session, string token, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/logout-session";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent("{}", Encoding.UTF8, "application/json");

        var res = await _http.SendAsync(req, ct);
        _log.LogInformation("[WPPConnect-MGMT] LogoutSession {Status}", (int)res.StatusCode);
    }

    public async Task DeleteSessionAsync(string session, string token, CancellationToken ct = default)
    {
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/delete-session";
        using var req = new HttpRequestMessage(HttpMethod.Delete, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        try
        {
            var res = await _http.SendAsync(req, ct);
            _log.LogInformation("[WPPConnect-MGMT] DeleteSession {Status}", (int)res.StatusCode);
        }
        catch (Exception ex)
        {
            _log.LogWarning("[WPPConnect-MGMT] DeleteSession failed (ignored): {Msg}", ex.Message);
        }
    }

    public async Task<string> RequestPairingCodeAsync(string session, string token, string phoneNumber, CancellationToken ct = default)
    {
        // Session must be in INITIALIZING state (started but no QR scanned yet)
        var url = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{session}/request-pairing-code";
        using var req = new HttpRequestMessage(HttpMethod.Post, url);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        req.Content = new StringContent(
            JsonSerializer.Serialize(new { phoneNumber }),
            Encoding.UTF8, "application/json");

        _log.LogInformation("[WPPConnect-MGMT] RequestPairingCode → {Phone}", phoneNumber);
        var res  = await _http.SendAsync(req, ct);
        var body = await res.Content.ReadAsStringAsync(ct);
        _log.LogInformation("[WPPConnect-MGMT] PairingCode {Status}: {Body}", (int)res.StatusCode, body);

        if (!res.IsSuccessStatusCode)
            throw new InvalidOperationException($"Pairing code alınamadı: {body}");

        using var doc = JsonDocument.Parse(body);
        // Try common field names across WppConnect versions
        foreach (var field in new[] { "pairingCode", "code", "data" })
        {
            if (doc.RootElement.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.String)
            {
                var code = v.GetString() ?? "";
                if (!string.IsNullOrWhiteSpace(code)) return code;
            }
        }
        // Some versions nest under "response"
        if (doc.RootElement.TryGetProperty("response", out var resp))
        {
            foreach (var field in new[] { "pairingCode", "code" })
            {
                if (resp.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    var code = v.GetString() ?? "";
                    if (!string.IsNullOrWhiteSpace(code)) return code;
                }
            }
        }

        throw new InvalidOperationException($"Pairing code alanı bulunamadı: {body}");
    }
}
