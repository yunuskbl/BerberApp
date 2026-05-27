using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.Commands;
using BerberApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Tenant.Handlers;

public class CreateBranchHandler : IRequestHandler<CreateBranchCommand, CreateBranchResult>
{
    private readonly IAppDbContext _context;

    public CreateBranchHandler(IAppDbContext context) => _context = context;

    public async Task<CreateBranchResult> Handle(CreateBranchCommand request, CancellationToken ct)
    {
        var subdomain = request.Subdomain.Trim().ToLower();

        if (await _context.Tenants.AnyAsync(t => t.Subdomain == subdomain && !t.IsDeleted, ct))
            throw new InvalidOperationException("Bu subdomain zaten kullanımda.");

        var branch = new BerberApp.Domain.Entities.Tenant
        {
            Name           = request.Name.Trim(),
            Subdomain      = subdomain,
            Phone          = request.Phone,
            Address        = request.Address,
            City           = request.City,
            ParentTenantId = request.ParentTenantId,
            IsActive       = true,
        };

        _context.Tenants.Add(branch);
        await _context.SaveChangesAsync(ct);

        return new CreateBranchResult { Id = branch.Id, Name = branch.Name, Subdomain = branch.Subdomain };
    }
}
