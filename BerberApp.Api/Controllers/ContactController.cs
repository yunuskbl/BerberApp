using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ContactController : BaseApiController
{
    private readonly IAppDbContext _context;

    public ContactController(IMediator mediator, IAppDbContext context)
        : base(mediator)
    {
        _context = context;
    }

    /// <summary>
    /// Aktif ödeme yöntemlerini getir (anonim erişim — salon sahipleri görebilir)
    /// </summary>
    [AllowAnonymous]
    [HttpGet("payment-methods")]
    public async Task<IActionResult> GetActivePaymentMethods()
    {
        var methods = await _context.PaymentMethods
            .IgnoreQueryFilters()
            .Where(p => !p.IsDeleted && p.IsActive)
            .OrderBy(p => p.Order)
            .Select(p => new
            {
                p.Id, p.Name, p.BankName, p.Iban,
                p.AccountHolder, p.Description
            })
            .ToListAsync();

        return Ok(new { success = true, data = methods });
    }

    /// <summary>
    /// Salon admininin kendi mesajlarını (ve SuperAdmin yanıtlarını) getirir
    /// </summary>
    [Authorize]
    [HttpGet("messages")]
    public async Task<IActionResult> GetMyMessages()
    {
        var messages = await _context.ContactMessages
            .IgnoreQueryFilters()
            .Where(m => m.TenantId == TenantId && !m.IsDeleted)
            .OrderByDescending(m => m.CreatedAt)
            .Select(m => new
            {
                m.Id, m.Subject, m.Message, m.Status,
                m.Reply, m.RepliedAt, m.IsReplySeen, m.CreatedAt
            })
            .ToListAsync();

        return Ok(new { success = true, data = messages });
    }

    /// <summary>
    /// Salon admini yanıtı gördü olarak işaretler (bildirim kapanır)
    /// </summary>
    [Authorize]
    [HttpPatch("messages/{id}/seen")]
    public async Task<IActionResult> MarkReplySeen(Guid id)
    {
        var msg = await _context.ContactMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == id && m.TenantId == TenantId && !m.IsDeleted);

        if (msg == null) return NotFound(new { success = false });

        if (!msg.IsReplySeen && msg.Reply != null)
        {
            msg.IsReplySeen = true;
            await _context.SaveChangesAsync();
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Salon admini mesaj gönderir
    /// </summary>
    [Authorize]
    [HttpPost("messages")]
    public async Task<IActionResult> SendMessage([FromBody] SendMessageRequest req)
    {
        if (string.IsNullOrWhiteSpace(req.Subject) || string.IsNullOrWhiteSpace(req.Message))
            return BadRequest(new { success = false, message = "Konu ve mesaj zorunludur." });

        var tenant = await _context.Tenants
            .IgnoreQueryFilters()
            .Select(t => new { t.Id, t.Name })
            .FirstOrDefaultAsync(t => t.Id == TenantId);

        var senderEmail = User.Claims
            .FirstOrDefault(c => c.Type == "email")?.Value ?? "";

        var msg = new ContactMessage
        {
            TenantId = TenantId,
            TenantName = tenant?.Name ?? "",
            SenderEmail = senderEmail,
            Subject = req.Subject,
            Message = req.Message,
            Status = "New"
        };

        _context.ContactMessages.Add(msg);
        await _context.SaveChangesAsync();

        return Ok(new { success = true, message = "Mesajınız iletildi." });
    }
}

public class SendMessageRequest
{
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
