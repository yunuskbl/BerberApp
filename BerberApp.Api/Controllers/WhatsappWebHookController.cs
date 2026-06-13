using System.Text.Json;
using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Settings;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/webhook")]
[AllowAnonymous]
public class WhatsappWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _context;
    private readonly ILogger<WhatsappWebhookController> _logger;
    private readonly string _verifyToken;

    public WhatsappWebhookController(
        IMediator mediator,
        IAppDbContext context,
        ILogger<WhatsappWebhookController> logger,
        IOptions<MetaWhatsAppSettings> metaOptions)
    {
        _mediator    = mediator;
        _context     = context;
        _logger      = logger;
        _verifyToken = metaOptions.Value.WebhookVerifyToken;
    }

    /// <summary>
    /// Meta Developer Console'dan webhook doğrulama isteği.
    /// hub.verify_token eşleşirse hub.challenge değerini döner.
    /// </summary>
    [HttpGet("whatsapp")]
    public IActionResult Verify(
        [FromQuery(Name = "hub.mode")]         string? mode,
        [FromQuery(Name = "hub.challenge")]    string? challenge,
        [FromQuery(Name = "hub.verify_token")] string? verifyToken)
    {
        if (mode == "subscribe" && verifyToken == _verifyToken && challenge is not null)
        {
            _logger.LogInformation("Meta webhook doğrulandı.");
            return Ok(challenge);
        }

        _logger.LogWarning("Geçersiz webhook doğrulama isteği. mode={Mode} token={Token}", mode, verifyToken);
        return Forbid();
    }

    /// <summary>
    /// Meta'nın gönderdiği gelen mesajlar.
    /// İşletme sahibi ONAYLA N veya REDDET N yazınca randevu güncellenir.
    /// </summary>
    [HttpPost("whatsapp")]
    public async Task<IActionResult> HandleIncoming([FromBody] JsonElement body)
    {
        // Meta her zaman 200 bekler — hata olsa bile 200 dön, loglayıp geç
        try
        {
            var messages = ExtractMessages(body);

            foreach (var (from, text) in messages)
            {
                await ProcessMessageAsync(from, text);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook işlenirken hata oluştu.");
        }

        return Ok(new { });
    }

    // ── İç yardımcılar ───────────────────────────────────────────────────────

    private async Task ProcessMessageAsync(string from, string text)
    {
        var senderPhone = NormalizePhone(from);

        // Gönderen numaraya sahip aktif tenant'ı bul
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.NotificationPhone != null &&
                                      x.NotificationPhone == senderPhone &&
                                      x.IsActive);

        if (tenant is null)
        {
            _logger.LogInformation("Webhook: Kayıtlı tenant bulunamadı. Telefon: {Phone}", senderPhone);
            return;
        }

        var message   = text.Trim();
        bool isConfirm = message.StartsWith("ONAYLA ", StringComparison.OrdinalIgnoreCase);
        bool isCancel  = message.StartsWith("REDDET ", StringComparison.OrdinalIgnoreCase);

        if (!isConfirm && !isCancel)
        {
            _logger.LogInformation("Webhook: Bilinmeyen komut '{Message}' — TenantId={TenantId}", message, tenant.Id);
            return;
        }

        var numberStr = (isConfirm ? message["ONAYLA ".Length..] : message["REDDET ".Length..]).Trim();
        if (!int.TryParse(numberStr, out int number) || number < 1)
        {
            _logger.LogWarning("Webhook: Geçersiz sıra numarası '{Num}'", numberStr);
            return;
        }

        var pendingAppointments = await _context.Appointments
            .Where(x => x.TenantId == tenant.Id && x.Status == AppointmentStatus.Pending)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var appointment = pendingAppointments.ElementAtOrDefault(number - 1);
        if (appointment is null)
        {
            _logger.LogWarning("Webhook: #{Number} numaralı bekleyen randevu yok — TenantId={TenantId}", number, tenant.Id);
            return;
        }

        if (isConfirm)
        {
            await _mediator.Send(new ConfirmAppointmentCommand { Id = appointment.Id, TenantId = tenant.Id });
            _logger.LogInformation("Webhook ile randevu onaylandı: {AppointmentId}", appointment.Id);
        }
        else
        {
            await _mediator.Send(new CancelAppointmentCommand { Id = appointment.Id, TenantId = tenant.Id });
            _logger.LogInformation("Webhook ile randevu reddedildi: {AppointmentId}", appointment.Id);
        }
    }

    /// <summary>Meta webhook payload'undan (from, text) çiftlerini çıkarır.</summary>
    private static List<(string from, string text)> ExtractMessages(JsonElement root)
    {
        var result = new List<(string, string)>();

        if (!root.TryGetProperty("entry", out var entries)) return result;

        foreach (var entry in entries.EnumerateArray())
        {
            if (!entry.TryGetProperty("changes", out var changes)) continue;

            foreach (var change in changes.EnumerateArray())
            {
                if (!change.TryGetProperty("value", out var value)) continue;
                if (!value.TryGetProperty("messages", out var messages)) continue;

                foreach (var msg in messages.EnumerateArray())
                {
                    if (!msg.TryGetProperty("type", out var typeEl) || typeEl.GetString() != "text") continue;
                    if (!msg.TryGetProperty("from", out var fromEl)) continue;
                    if (!msg.TryGetProperty("text", out var textObj)) continue;
                    if (!textObj.TryGetProperty("body", out var bodyEl)) continue;

                    var from = fromEl.GetString() ?? string.Empty;
                    var text = bodyEl.GetString() ?? string.Empty;

                    if (!string.IsNullOrWhiteSpace(from) && !string.IsNullOrWhiteSpace(text))
                        result.Add((from, text));
                }
            }
        }

        return result;
    }

    private static string NormalizePhone(string from)
    {
        // Meta gönderir: "905551234567" (E.164, + olmadan)
        // Tenant.NotificationPhone formatı: "05551234567"
        var phone = from.Replace("+", "").Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("90") && phone.Length > 10)
            return "0" + phone[2..];
        return phone;
    }
}
