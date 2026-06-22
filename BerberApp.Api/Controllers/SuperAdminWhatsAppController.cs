using BerberApp.Application.Common.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;
using System.Text.Json;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/superadmin/whatsapp")]
[Authorize(Roles = "SuperAdmin")]
public class SuperAdminWhatsAppController : ControllerBase
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly WppConnectSettings _cfg;
    private readonly ILogger<SuperAdminWhatsAppController> _logger;

    public SuperAdminWhatsAppController(
        IHttpClientFactory httpFactory,
        IOptions<WppConnectSettings> options,
        ILogger<SuperAdminWhatsAppController> logger)
    {
        _httpFactory = httpFactory;
        _cfg         = options.Value;
        _logger      = logger;
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
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{_cfg.Token}/check-connection-session";
            var res  = await http.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(body);
            var status = doc.RootElement.TryGetProperty("status", out var s) ? s.GetString() : null;
            var connected = status == "CONNECTED";

            return Ok(new { connected, status = status ?? "UNKNOWN", raw = body });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WppConnect status check failed");
            return Ok(new { connected = false, status = "ERROR", message = ex.Message });
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

        // 2. Session başlat, QR gelecek
        try
        {
            var startUrl  = $"{base_}/api/{_cfg.Session}/{_cfg.Token}/start-session";
            var startBody = new StringContent("{\"waitQrCode\":true}", System.Text.Encoding.UTF8, "application/json");
            var startRes  = await http.PostAsync(startUrl, startBody);
            var startStr  = await startRes.Content.ReadAsStringAsync();

            if (TryExtractQr(startStr, out var qrFromStart))
                return Ok(new { success = true, qr = qrFromStart });

            // 3. Kısa bekleme sonra tekrar dene
            await Task.Delay(2000);
            var qr2 = await TryGetQrCode(http, base_);
            if (qr2 != null) return Ok(new { success = true, qr = qr2 });

            return Ok(new { success = false, message = "QR henüz hazır değil. Birkaç saniye sonra tekrar deneyin." });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WppConnect QR/start failed");
            return Ok(new { success = false, message = "WppConnect bağlantı hatası." });
        }
    }

    private async Task<string?> TryGetQrCode(HttpClient http, string baseUrl)
    {
        var url = $"{baseUrl}/api/{_cfg.Session}/{_cfg.Token}/qrcode-session";
        var res = await http.GetAsync(url);
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
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{_cfg.Token}/start-session";
            var res  = await http.PostAsync(url, new StringContent("{\"waitQrCode\":true}", System.Text.Encoding.UTF8, "application/json"));
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
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{_cfg.Token}/close-session";
            var res  = await http.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
            var body = await res.Content.ReadAsStringAsync();
            return Ok(new { success = true, raw = body });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Yeni token üretir (SecretKey gerektirir)</summary>
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

            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("token", out var t))
                return Ok(new { success = true, token = t.GetString(), message = "Token oluşturuldu. appsettings.json → WppConnect:Token alanını güncelleyin." });

            return Ok(new { success = false, message = "Token alınamadı.", raw = body });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }
}
