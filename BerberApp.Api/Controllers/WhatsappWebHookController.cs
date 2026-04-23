using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/webhook")]
[AllowAnonymous]
public class WhatsappWebhookController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _context;

    public WhatsappWebhookController(IMediator mediator, IAppDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [HttpPost("whatsapp")]
    public async Task<IActionResult> HandleIncoming([FromForm] string From, [FromForm] string Body)
    {
        if (string.IsNullOrWhiteSpace(From) || string.IsNullOrWhiteSpace(Body))
            return TwimlResponse("Geçersiz istek.");

        // "whatsapp:+905383996916" → "05383996916"
        var senderPhone = NormalizePhone(From);

        // Gönderen numaraya sahip tenant'ı bul
        var tenant = await _context.Tenants
            .FirstOrDefaultAsync(x => x.NotificationPhone != null && x.NotificationPhone == senderPhone && x.IsActive);

        if (tenant is null)
            return TwimlResponse("Bu numara kayıtlı değil.");

        var message = Body.Trim();

        bool isConfirm = message.StartsWith("ONAYLA ", StringComparison.OrdinalIgnoreCase);
        bool isCancel = message.StartsWith("REDDET ", StringComparison.OrdinalIgnoreCase);
        if (!isConfirm && !isCancel)
            return TwimlResponse("Bilinmeyen komut.\nOnaylamak: ONAYLA [numara]\nReddetmek: REDDET [numara]");

        var numberStr = (isConfirm ? message["ONAYLA ".Length..] : message["REDDET ".Length..]).Trim();


        if (!int.TryParse(numberStr, out int number) || number < 1)
            return TwimlResponse("Geçersiz numara.");

        var pendingAppointments = await _context.Appointments
            .Where(x => x.TenantId == tenant.Id && x.Status == AppointmentStatus.Pending)
            .OrderBy(x => x.StartTime)
            .ToListAsync();

        var appointment = pendingAppointments.ElementAtOrDefault(number - 1);

        if (appointment is null)
            return TwimlResponse($"#{number} numaralı bekleyen randevu bulunamadı.");

        if (isConfirm)
        {
            await _mediator.Send(new ConfirmAppointmentCommand { Id = appointment.Id, TenantId = tenant.Id });
            Console.WriteLine($"[WEBHOOK] Randevu onaylandı: {appointment.Id}");
            return TwimlResponse("✅ Randevu onaylandı! Müşteriye bildirim gönderildi.");
        }
        else
        {
            await _mediator.Send(new CancelAppointmentCommand { Id = appointment.Id, TenantId = tenant.Id });
            Console.WriteLine($"[WEBHOOK] Randevu reddedildi: {appointment.Id}");
            return TwimlResponse("❌ Randevu reddedildi. Müşteriye bildirim gönderildi.");
        }
    }

    private static string NormalizePhone(string from)
    {
        // "whatsapp:+905383996916" → "05383996916"
        var phone = from.Replace("whatsapp:", "").Replace(" ", "").Replace("-", "");
        if (phone.StartsWith("+90"))
            phone = "0" + phone[3..];
        return phone;
    }

    private ContentResult TwimlResponse(string message)
    {
        var twiml = $"""
            <?xml version="1.0" encoding="UTF-8"?>
            <Response>
                <Message>{message}</Message>
            </Response>
            """;
        return Content(twiml, "text/xml");
    }
}
