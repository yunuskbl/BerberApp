using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.Commands;
using BerberApp.Application.Tenant.DTOs;
using MediatR;

namespace BerberApp.Application.Tenant.Handlers;

public class UpdateTenantHandler : IRequestHandler<UpdateTenantCommand, TenantDto>
{
    private readonly IGenericRepository<TenantEntity> _tenantRepo;

    public UpdateTenantHandler(IGenericRepository<TenantEntity> tenantRepo)
    {
        _tenantRepo = tenantRepo;
    }

    public async Task<TenantDto> Handle(UpdateTenantCommand request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(request.Id, ct);

        if (tenant is null)
            throw new NotFoundException("Tenant", request.Id);

        tenant.Name = request.Name;
        tenant.LogoUrl = request.LogoUrl;
        tenant.Phone = request.Phone;
        tenant.Address = request.Address;

        await _tenantRepo.UpdateAsync(tenant, ct);

        return ToDto(tenant);
    }

    private static TenantDto ToDto(TenantEntity t) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Subdomain = t.Subdomain,
        LogoUrl = t.LogoUrl,
        Phone = t.Phone,
        Address = t.Address,
        IsActive = t.IsActive
    };
}