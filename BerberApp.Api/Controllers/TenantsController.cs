using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BerberApp.Application.Tenant.Commands;
using BerberApp.Application.Tenant.Queries;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using BerberApp.Domain.Enums;

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
        var branch = await _context.Tenants
            .FirstOrDefaultAsync(t => t.Id == id && t.ParentTenantId == TenantId && !t.IsDeleted);

        if (branch is null)
            return NotFound(new { success = false, message = "Şube bulunamadı." });

        _context.Tenants.Remove(branch);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
