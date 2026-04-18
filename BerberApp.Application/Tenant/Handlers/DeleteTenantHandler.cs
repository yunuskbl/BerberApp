using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.Commands;
using MediatR;

namespace BerberApp.Application.Tenant.Handlers;

public class DeleteTenantHandler : IRequestHandler<DeleteTenantCommand, bool>
{
    private readonly IGenericRepository<TenantEntity> _tenantRepo;

    public DeleteTenantHandler(IGenericRepository<TenantEntity> tenantRepo)
    {
        _tenantRepo = tenantRepo;
    }

    public async Task<bool> Handle(DeleteTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(request.Id, ct);

        if (tenant is null)
            throw new NotFoundException("Tenant", request.Id);

        await _tenantRepo.DeleteAsync(tenant, ct);
        return true;
    }
}