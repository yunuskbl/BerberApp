using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BerberApp.Application.Tenant.Commands;
using BerberApp.Application.Tenant.Queries;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BerberApp.Domain.Enums;
using BerberApp.Domain.Entities;

namespace BerberApp.Api.Controllers;

public class TenantsController : BaseApiController
{
    private readonly IAppDbContext _context;

    public TenantsController(IMediator mediator, IAppDbContext context) : base(mediator)
    {
        _context = context;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyTenant()
        => Success(await Mediator.Send(new GetTenantByIdQuery { Id = TenantId }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTenantCommand command)
    => Success(await Mediator.Send(command));

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Success(await Mediator.Send(new GetAllTenantsQuery()));

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateTenantCommand command)
    {
        command.Id = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpDelete]
    public async Task<IActionResult> Delete()
    {
        await Mediator.Send(new DeleteTenantCommand { Id = TenantId });
        return NoContent();
    }

    [HttpPost("logo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadLogo([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Dosya seçilmedi." });

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { success = false, message = "Sadece JPG, PNG veya WebP yükleyebilirsiniz." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { success = false, message = "Dosya boyutu 5MB'yi geçemez." });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "logos");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var fileName = $"{TenantId}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        var logoUrl = $"/uploads/logos/{fileName}";

        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);
        if (tenant != null)
        {
            tenant.LogoUrl = logoUrl;
            await _context.SaveChangesAsync();
        }

        return Success(new { logoUrl });
    }

    // ── PHOTOS ──────────────────────────────────────────────

    [HttpGet("photos")]
    public async Task<IActionResult> GetPhotos()
    {
        var photos = await _context.TenantPhotos
            .Where(x => x.TenantId == TenantId)
            .OrderBy(x => x.Order)
            .Select(x => new { x.Id, x.Url, x.Order })
            .ToListAsync();

        return Success(new { photos });
    }

    [HttpPost("photos")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UploadPhoto([FromForm] IFormFile file)
    {
        var count = await _context.TenantPhotos.CountAsync(x => x.TenantId == TenantId);
        if (count >= 6)
            return BadRequest(new { success = false, message = "En fazla 6 fotoğraf yükleyebilirsiniz." });

        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Dosya seçilmedi." });

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { success = false, message = "Sadece JPG, PNG veya WebP yükleyebilirsiniz." });

        if (file.Length > 5 * 1024 * 1024)
            return BadRequest(new { success = false, message = "Dosya boyutu 5MB'yi geçemez." });

        var uploadsDir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "photos");
        Directory.CreateDirectory(uploadsDir);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(uploadsDir, fileName);

        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        var photo = new BerberApp.Domain.Entities.TenantPhoto
        {
            TenantId = TenantId,
            Url = $"/uploads/photos/{fileName}",
            Order = count
        };

        _context.TenantPhotos.Add(photo);
        await _context.SaveChangesAsync();

        return Success(new { photo = new { photo.Id, photo.Url, photo.Order } });
    }

    [HttpDelete("photos/{id}")]
    public async Task<IActionResult> DeletePhoto(Guid id)
    {
        var photo = await _context.TenantPhotos
            .FirstOrDefaultAsync(x => x.Id == id && x.TenantId == TenantId);

        if (photo is null)
            return NotFound(new { success = false, message = "Fotoğraf bulunamadı." });

        _context.TenantPhotos.Remove(photo);
        await _context.SaveChangesAsync();

        return Success(new { message = "Fotoğraf silindi." });
    }

    // ── Salon Kapalı Günler ──────────────────────────────────────────────────

    [HttpGet("closures")]
    public async Task<IActionResult> GetClosures()
    {
        var closures = await _context.TenantClosures
            .Where(c => c.TenantId == TenantId && !c.IsDeleted)
            .OrderBy(c => c.StartDate)
            .Select(c => new {
                c.Id,
                StartDate = c.StartDate.ToString("yyyy-MM-dd"),
                EndDate   = c.EndDate.ToString("yyyy-MM-dd"),
                c.Reason
            })
            .ToListAsync();

        return Success(closures);
    }

    [HttpPost("closures")]
    public async Task<IActionResult> AddClosure([FromBody] AddClosureRequest request)
    {
        if (!DateOnly.TryParse(request.StartDate, out var start))
            return BadRequest(new { success = false, message = "Geçersiz başlangıç tarihi." });

        if (!DateOnly.TryParse(request.EndDate, out var end))
            end = start;

        if (end < start)
            return BadRequest(new { success = false, message = "Bitiş tarihi başlangıç tarihinden önce olamaz." });

        var closure = new BerberApp.Domain.Entities.TenantClosure
        {
            TenantId  = TenantId,
            StartDate = start,
            EndDate   = end,
            Reason    = request.Reason,
        };

        _context.TenantClosures.Add(closure);
        await _context.SaveChangesAsync();

        return Created(new {
            closure.Id,
            StartDate = closure.StartDate.ToString("yyyy-MM-dd"),
            EndDate   = closure.EndDate.ToString("yyyy-MM-dd"),
            closure.Reason
        });
    }

    [HttpDelete("closures/{id}")]
    public async Task<IActionResult> DeleteClosure(Guid id)
    {
        var closure = await _context.TenantClosures
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == TenantId && !c.IsDeleted);

        if (closure is null)
            return NotFound(new { success = false, message = "Kapalı gün bulunamadı." });

        _context.TenantClosures.Remove(closure);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    public record AddClosureRequest(string StartDate, string EndDate, string? Reason);

    // ── BRANCHES ─────────────────────────────────────────────────────────────

    [HttpGet("branches")]
    public async Task<IActionResult> GetBranches()
    {
        var currentTenant = await _context.Tenants.FindAsync(TenantId);
        if (currentTenant?.ParentTenantId != null)
            return StatusCode(403, new { success = false, message = "Şubeler kendi alt şube oluşturamaz." });

        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == TenantId && !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription?.Plan != PlanType.Premium)
            return StatusCode(403, new { success = false, message = "Çoklu şube yönetimi sadece Premium planda kullanılabilir." });

        var branches = await Mediator.Send(new GetBranchesQuery { ParentTenantId = TenantId });
        return Success(branches);
    }

    [HttpPost("branches")]
    public async Task<IActionResult> CreateBranch([FromBody] CreateBranchCommand command)
    {
        var currentTenant = await _context.Tenants.FindAsync(TenantId);
        if (currentTenant?.ParentTenantId != null)
            return StatusCode(403, new { success = false, message = "Şubeler kendi alt şube oluşturamaz." });

        var subscription = await _context.Subscriptions
            .Where(s => s.TenantId == TenantId && !s.IsDeleted)
            .OrderByDescending(s => s.StartDate)
            .FirstOrDefaultAsync();

        if (subscription?.Plan != PlanType.Premium)
            return StatusCode(403, new { success = false, message = "Çoklu şube yönetimi sadece Premium planda kullanılabilir." });

        command.ParentTenantId = TenantId;
        var result = await Mediator.Send(command);
        return Created(result);
    }

    [HttpDelete("branches/{id}")]
    public async Task<IActionResult> DeleteBranch(Guid id)
    {
        var currentTenant = await _context.Tenants.FindAsync(TenantId);
        if (currentTenant?.ParentTenantId != null)
            return StatusCode(403, new { success = false, message = "Şubeler kendi alt şube oluşturamaz." });

        var branch = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id && t.ParentTenantId == TenantId && !t.IsDeleted);

        if (branch is null)
            return NotFound(new { success = false, message = "Şube bulunamadı." });

        _context.Tenants.Remove(branch);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    [HttpPost("test-whatsapp")]
    public async Task<IActionResult> TestWhatsApp(
        [FromBody] TestWhatsAppRequest req,
        [FromServices] IWhatsAppService whatsApp,
        [FromServices] IWppConnectManagementService mgmt)
    {
        if (string.IsNullOrWhiteSpace(req.Phone))
            return BadRequest(new { success = false, message = "Telefon numarası gerekli." });

        try
        {
            var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);

            if (string.IsNullOrWhiteSpace(tenant?.WppConnectSession) || string.IsNullOrWhiteSpace(tenant?.WppConnectToken))
                return Ok(new { success = false, message = "Önce WhatsApp'ı bağlayın (Ayarlar → WhatsApp Hesabınızı Bağlayın)." });

            // Session durumunu kontrol et
            var status = await mgmt.GetStatusAsync(tenant.WppConnectSession, tenant.WppConnectToken);
            if (status is not ("CONNECTED" or "inChat" or "isLogged"))
                return Ok(new { success = false, message = $"WhatsApp oturumu bağlı değil (durum: {status}). Lütfen yeniden bağlanın." });

            var wa = whatsApp.ForTenant(tenant.WppConnectSession, tenant.WppConnectToken);
            await wa.SendCustomMessageAsync(req.Phone, req.Message);
            return Success<object>(null!, "WhatsApp mesajı gönderildi.");
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    // ── WhatsApp Oturum Yönetimi ─────────────────────────────────────────────

    [HttpPost("whatsapp/connect")]
    public async Task<IActionResult> WhatsAppConnect(
        [FromServices] IWppConnectManagementService mgmt)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);
        if (tenant is null) return NotFound();

        var session = $"tenant-{tenant.Subdomain}";
        try
        {
            // Full cleanup before reconnecting so WppConnect can't auto-connect from cached files:
            // logout → clears WhatsApp credentials from memory
            // close  → stops headless browser process
            // delete → removes token files from disk (the root cause of ghost sessions)
            if (!string.IsNullOrWhiteSpace(tenant.WppConnectSession) && !string.IsNullOrWhiteSpace(tenant.WppConnectToken))
            {
                try { await mgmt.LogoutSessionAsync(tenant.WppConnectSession, tenant.WppConnectToken); }
                catch { /* ignore */ }
                try { await mgmt.CloseSessionAsync(tenant.WppConnectSession, tenant.WppConnectToken); }
                catch { /* ignore */ }
                try { await mgmt.DeleteSessionAsync(tenant.WppConnectSession, tenant.WppConnectToken); }
                catch { /* ignore */ }
                await Task.Delay(1500);
            }

            var token  = await mgmt.GenerateTokenAsync(session);
            var result = await mgmt.StartSessionAsync(session, token);

            tenant.WppConnectSession = session;
            tenant.WppConnectToken   = token;
            await _context.SaveChangesAsync();

            if (result.IsConnected)
                return Success(new { session, qrCode = (string?)null, isConnected = true });

            return Success(new { session, qrCode = result.QrCode, isConnected = false });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("whatsapp/qr")]
    public async Task<IActionResult> WhatsAppQr(
        [FromServices] IWppConnectManagementService mgmt)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);
        if (string.IsNullOrWhiteSpace(tenant?.WppConnectSession))
            return Ok(new { success = false, message = "Oturum başlatılmamış." });

        try
        {
            var qr = await mgmt.GetQrCodeAsync(tenant.WppConnectSession, tenant.WppConnectToken!);
            return Success(new { qrCode = qr });
        }
        catch (Exception ex)
        {
            return Ok(new { success = false, message = ex.Message });
        }
    }

    [HttpGet("whatsapp/status")]
    public async Task<IActionResult> WhatsAppStatus(
        [FromServices] IWppConnectManagementService mgmt)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);
        if (string.IsNullOrWhiteSpace(tenant?.WppConnectSession))
            return Success(new { status = "DISCONNECTED", isConnected = false, hasSession = false });

        var status = await mgmt.GetStatusAsync(tenant.WppConnectSession, tenant.WppConnectToken!);
        return Success(new
        {
            status,
            isConnected = status is "CONNECTED" or "inChat" or "isLogged",
            hasSession  = true,
            session     = tenant.WppConnectSession
        });
    }

    [HttpDelete("whatsapp/disconnect")]
    public async Task<IActionResult> WhatsAppDisconnect(
        [FromServices] IWppConnectManagementService mgmt)
    {
        var tenant = await _context.Tenants.FirstOrDefaultAsync(x => x.Id == TenantId);
        if (tenant is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(tenant.WppConnectSession))
        {
            try { await mgmt.CloseSessionAsync(tenant.WppConnectSession, tenant.WppConnectToken!); }
            catch { /* sunucu erişilemese bile DB'yi temizle */ }

            tenant.WppConnectSession = null;
            tenant.WppConnectToken   = null;
            await _context.SaveChangesAsync();
        }

        return Success<object>(null!, "WhatsApp bağlantısı kesildi.");
    }
}

public record TestWhatsAppRequest(string Phone, string Message);
