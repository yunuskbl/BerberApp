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

    /// <summary>QR kod base64 olarak döner</summary>
    [HttpGet("qr")]
    public async Task<IActionResult> GetQr()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
            return BadRequest(new { success = false, message = "Token yapılandırılmamış. Önce token oluşturun." });

        try
        {
            var http = _httpFactory.CreateClient("wppconnect");
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{_cfg.Token}/qrcode-session";
            var res  = await http.GetAsync(url);
            var body = await res.Content.ReadAsStringAsync();

            // WppConnect ya image döner ya da JSON içinde base64
            var ct = res.Content.Headers.ContentType?.MediaType ?? "";
            if (ct.StartsWith("image/"))
            {
                var bytes  = await res.Content.ReadAsByteArrayAsync();
                var base64 = Convert.ToBase64String(bytes);
                return Ok(new { success = true, qr = $"data:{ct};base64,{base64}" });
            }

            // JSON response — qr alanını çek
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("qrcode", out var qr))
                return Ok(new { success = true, qr = qr.GetString() });
            if (doc.RootElement.TryGetProperty("qr", out var qr2))
                return Ok(new { success = true, qr = qr2.GetString() });

            return Ok(new { success = false, message = "QR alınamadı. Bağlantı zaten kurulu olabilir.", raw = body });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WppConnect QR fetch failed");
            return Ok(new { success = false, message = ex.Message });
        }
    }

    /// <summary>Oturumu başlatır / yeniden başlatır</summary>
    [HttpPost("start")]
    public async Task<IActionResult> StartSession()
    {
        if (string.IsNullOrWhiteSpace(_cfg.Token))
            return BadRequest(new { success = false, message = "Token yapılandırılmamış." });

        try
        {
            var http = _httpFactory.CreateClient("wppconnect");
            var url  = $"{_cfg.BaseUrl.TrimEnd('/')}/api/{_cfg.Session}/{_cfg.Token}/start-session";
            var res  = await http.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
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
