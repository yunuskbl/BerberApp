using BerberApp.Application.Common.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Net.Http.Headers;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/superadmin/whatsapp")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminWhatsAppController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IOptionsMonitor<WppConnectSettings> _cfgMonitor;
    private WppConnectSettings _cfg => _cfgMonitor.CurrentValue;
    private readonly ILogger<SuperAdminWhatsAppController> _logger;
    private readonly IWebHostEnvironment _env;

    public SuperAdminWhatsAppController(
        IHttpClientFactory httpFactory,
        IOptionsMonitor<WppConnectSettings> options,
        ILogger<SuperAdminWhatsAppController> logger,
        IWebHostEnvironment env)
    {
        _httpFactory  = httpFactory;
        _cfgMonitor   = options;
        _logger       = logger;
        _env          = env;
    }

    // Token'ı Bearer header olarak eklenmiş HttpRequestMessage oluşturur
    private HttpRequestMessage WppRequest(HttpMethod method, string url)
    {
        var req = new HttpRequestMessage(method, url);
        if (!string.IsNullOrWhiteSpace(_cfg.Token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _cfg.Token);
        return req;
    }

    /// <summary>Bağlantı durumunu döner</summary>
    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
            return Ok(new { connected = false, status = "NO_TOKEN", message = "Token yapılandırılmamış." });

        try
        {
            var http = _httpFactory.CreateClient("wppconnect");
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/check-connection-session";
            var res  = await http.SendAsync(WppRequest(HttpMethod.Get, url));
            var body = await res.Content.ReadAsStringAsync();

            if (body.TrimStart().StartsWith('<'))
                return Ok(new { connected = false, status = "ERROR", message = "WppConnect erişilemiyor." });

            // Flexible parse — status string veya boolean olabilir
            bool connected = false;
            string statusStr = "UNKNOWN";

            try
            {
                using var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var s))
                {
                    if (s.ValueKind == JsonValueKind.String)
                    {
                        statusStr = s.GetString() ?? "UNKNOWN";
                        connected = statusStr.Equals("CONNECTED", StringComparison.OrdinalIgnoreCase)
                                 || statusStr.Equals("inChat",    StringComparison.OrdinalIgnoreCase);
                    }
                    else if (s.ValueKind == JsonValueKind.True)
                    {
                        statusStr = "CONNECTED"; connected = true;
                    }
                }

                // Alternatif alan adları
                if (!connected && root.TryGetProperty("connected", out var c))
                    connected = c.ValueKind == JsonValueKind.True;
            }
            catch
            {
                // JSON parse edilemedi; 200 OK ise bağlı say
                connected  = res.IsSuccessStatusCode;
                statusStr  = connected ? "CONNECTED" : "ERROR";
            }

            return Ok(new { connected, status = statusStr });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WppConnect status check failed");
            return Ok(new { connected = false, status = "ERROR", message = "WppConnect bağlantı hatası." });
        }
    }

    /// <summary>Oturumu başlatır ve QR kodu döner</summary>
    [HttpGet("qr")]
    public async Task<IActionResult> GetQr()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
            return Ok(new { success = false, message = "Token yapılandırılmamış." });

        var http  = _httpFactory.CreateClient("wppconnect");
        var base_ = _cfg.BaseUrl.TrimEnd('/');

        // 1. Önce mevcut QR'ı dene
        try
        {
            var qr = await TryGetQrCode(http, base_);
            if (qr != null) return Ok(new { success = true, qr });
        }
        catch { /* devam et */ }

        // 2. Session başlat
        try
        {
            var startUrl  = $"{base_}/api/{_cfg.Session}/start-session";
            var startReq  = WppRequest(HttpMethod.Post, startUrl);
            startReq.Content = new StringContent("{\"waitQrCode\":true}", System.Text.Encoding.UTF8, "application/json");
            var startRes  = await http.SendAsync(startReq);
            var startStr  = await startRes.Content.ReadAsStringAsync();

            if (TryExtractQr(startStr, out var qrFromStart))
                return Ok(new { success = true, qr = qrFromStart });
        }
        catch { /* devam et */ }

        // 3. QR hazır olana kadar poll et (max 20 saniye)
        for (int i = 0; i < 5; i++)
        {
            await Task.Delay(4000);
            try
            {
                var qr = await TryGetQrCode(http, base_);
                if (qr != null) return Ok(new { success = true, qr });
            }
            catch { /* devam et */ }
        }

        return Ok(new { success = false, message = "QR alınamadı. 'QR'ı Yenile' butonuna tekrar tıklayın." });
    }

    private async Task<string?> TryGetQrCode(HttpClient http, string baseUrl)
    {
        var url = $"{baseUrl}/api/{_cfg.Session}/qrcode-session";
        var res = await http.SendAsync(WppRequest(HttpMethod.Get, url));
        if (!res.IsSuccessStatusCode) return null;

        var ct = res.Content.Headers.ContentType?.MediaType ?? "";
        if (ct.StartsWith("image/"))
        {
            var bytes = await res.Content.ReadAsByteArrayAsync();
            return $"data:{ct};base64,{Convert.ToBase64String(bytes)}";
        }

        var body = await res.Content.ReadAsStringAsync();
        if (TryExtractQr(body, out var qr)) return qr;
        return null;
    }

    private static bool TryExtractQr(string json, out string? qr)
    {
        qr = null;
        if (string.IsNullOrWhiteSpace(json) || json.TrimStart().StartsWith('<')) return false;
        try
        {
            using var doc = JsonDocument.Parse(json);
            foreach (var field in new[] { "qrcode", "qr", "base64Qr" })
            {
                if (doc.RootElement.TryGetProperty(field, out var v) && v.ValueKind == JsonValueKind.String)
                {
                    qr = v.GetString();
                    return !string.IsNullOrEmpty(qr);
                }
            }
        }
        catch { /* HTML veya geçersiz JSON */ }
        return false;
    }

    /// <summary>Oturumu başlatır</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartSession()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
            return BadRequest(new { success = false, message = "Token yapılandırılmamış." });

        try
        {
            var http = _httpFactory.CreateClient("wppconnect");
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/start-session";
            var req  = WppRequest(HttpMethod.Post, url);
            req.Content = new StringContent("{\"waitQrCode\":true}", System.Text.Encoding.UTF8, "application/json");
            var res  = await http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            return Ok(new { success = true, raw = body });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>WhatsApp oturumunu kapatır</summary>
    [HttpPost("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
            return BadRequest(new { success = false, message = "Token yapılandırılmamış." });

        try
        {
            var http = _httpFactory.CreateClient("wppconnect");
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/close-session";
            var req  = WppRequest(HttpMethod.Post, url);
            req.Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json");
            var res  = await http.SendAsync(req);
            var body = await res.Content.ReadAsStringAsync();
            return Ok(new { success = true, raw = body });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Yeni token üretir (SecretKey gerektirir — URL'de gider, header gerekmez)</summary>
    [HttpPost("generate-token")]
    public async Task<IActionResult> GenerateToken()
    {
        if (string.IsNullOrWhiteSpace(_cfg.SecretKey))
            return BadRequest(new { success = false, message = "SecretKey yapılandırılmamış." });

        try
        {
            var http = _httpFactory.CreateClient("wppconnect");
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{_cfg.SecretKey}/generate-token";
            var res  = await http.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            var body = await res.Content.ReadAsStringAsync();

            if (body.TrimStart().StartsWith('<'))
                return Ok(new { success = false, message = "WppConnect erişilemiyor veya SecretKey hatalı." });

            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("token", out var t))
                return Ok(new { success = false, message = "Token alınamadı.", raw = body });

            var newToken = t.GetString()!;
            await SaveTokenToAppSettings(newToken);
            _logger.LogInformation("WppConnect token updated and saved");

            return Ok(new { success = true, token = newToken, message = "Token oluşturuldu ve kaydedildi. Şimdi QR'ı yükleyebilirsiniz." });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    private async Task SaveTokenToAppSettings(string token)
    {
        // Persist to uploads volume so token survives container rebuilds
        var persistPath = Path.Combine(_env.WebRootPath ?? "/app/wwwroot", "uploads", ".wppconnect.token");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(persistPath)!);
            await System.IO.File.WriteAllTextAsync(persistPath, token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token kalıcı dosyaya yazılamadı: {Path}", persistPath);
        }

        // Also update appsettings.json for IOptionsMonitor hot-reload
        var path = Path.Combine(_env.ContentRootPath, "appsettings.json");
        if (!System.IO.File.Exists(path)) return;

        var json = await System.IO.File.ReadAllTextAsync(path);
        var node = JsonNode.Parse(json)!;
        node["WppConnect"]!["Token"] = token;

        var opts = new JsonSerializerOptions { WriteIndented = true };
        await System.IO.File.WriteAllTextAsync(path, node.ToJsonString(opts));
    }
}
