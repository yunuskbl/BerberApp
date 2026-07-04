using BerberApp.Application.Customer.Commands;
using BerberApp.Application.Customer.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BerberApp.Api.Controllers;

[Authorize(Roles = "Admin,SuperAdmin")]
public class CustomersController : BaseApiController
{
    public CustomersController(IMediator mediator) : base(mediator) { }

    [HttpGet]
    public async Task<IActionResult> GetAll()
        => Success(await Mediator.Send(new GetAllCustomersQuery { TenantId = TenantId }));

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id)
        => Success(await Mediator.Send(new GetCustomerByIdQuery { Id = id, TenantId = TenantId }));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command)
    {
        command.TenantId = TenantId;
        return Created(await Mediator.Send(command));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCustomerCommand command)
    {
        command.Id = id;
        command.TenantId = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await Mediator.Send(new DeleteCustomerCommand { Id = id, TenantId = TenantId });
        return NoContent();
    }

    [HttpPost("broadcast")]
    public async Task<IActionResult> Broadcast([FromBody] BroadcastMessageCommand command)
    {
        command.TenantId = TenantId;
        return Success(await Mediator.Send(command));
    }

    [HttpPost("broadcast/image")]
    [RequestSizeLimit(5_242_880)]
    [RequestFormLimits(MultipartBodyLengthLimit = 5_242_880)]
    public async Task<IActionResult> UploadBroadcastImage([FromForm] IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "Dosya seçilmedi." });

        var allowed = new Dictionary<string, string>
        {
            ["image/jpeg"] = ".jpg",
            ["image/png"]  = ".png",
            ["image/webp"] = ".webp",
        };
        var ct = file.ContentType?.ToLowerInvariant() ?? "";
        if (!allowed.TryGetValue(ct, out var ext))
            return BadRequest(new { success = false, message = "Sadece JPG, PNG veya WebP yüklenebilir." });

        var dir = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "broadcast");
        Directory.CreateDirectory(dir);

        var fileName = $"{Guid.NewGuid()}{ext}";
        var filePath = Path.Combine(dir, fileName);
        await using var stream = System.IO.File.Create(filePath);
        await file.CopyToAsync(stream);

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var url = $"{baseUrl}/uploads/broadcast/{fileName}";

        return Success(new { url });
    }
}