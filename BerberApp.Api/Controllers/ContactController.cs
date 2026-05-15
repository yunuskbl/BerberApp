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
