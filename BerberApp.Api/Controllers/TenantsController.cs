using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using BerberApp.Application.Tenant.Commands;
using BerberApp.Application.Tenant.Queries;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

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

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteTenantCommand { Id = id });
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
}
