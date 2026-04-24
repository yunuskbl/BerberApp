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
    public async Task<IActionResult> UploadLogo([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Dosya seçilmedi." });

        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/webp" };
        if (!allowedTypes.Contains(file.ContentType.ToLower()))
            return BadRequest(new { success = false, message = "Sadece JPG, PNG veya WebP yükleyebilirsiniz." });

        if (file.Length > 2 * 1024 * 1024)
            return BadRequest(new { success = false, message = "Dosya boyutu 2MB'yi geçemez." });

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
}
