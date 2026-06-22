using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Tenant.Handlers;

public class DeleteTenantHandler : IRequestHandler<DeleteTenantCommand, bool>
{
    private readonly IGenericRepository<TenantEntity> _tenantRepo;
    private readonly IAppDbContext _context;

    public DeleteTenantHandler(
        IGenericRepository<TenantEntity> tenantRepo,
        IAppDbContext context)
    {
        _tenantRepo = tenantRepo;
        _context    = context;
    }

    public async Task<bool> Handle(DeleteTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(request.Id, ct);

        if (tenant is null)
            throw new NotFoundException("Tenant", request.Id);

        // Bağlı kullanıcıları da soft-delete et — böylece aynı telefon/mail ile
        // yeni kayıt yapılabilir.
        var users = await _context.Users
            .Where(x => x.TenantId == tenant.Id)
            .ToListAsync(ct);

        foreach (var user in users)
            user.IsDeleted = true;

        await _tenantRepo.DeleteAsync(tenant, ct);
        await _context.SaveChangesAsync(ct);
        return true;
    }
}
