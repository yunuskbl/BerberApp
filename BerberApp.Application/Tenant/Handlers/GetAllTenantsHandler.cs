using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.DTOs;
using BerberApp.Application.Tenant.Queries;
using MediatR;

namespace BerberApp.Application.Tenant.Handlers;

public class GetAllTenantsHandler : IRequestHandler<GetAllTenantsQuery, List<TenantDto>>
{
    private readonly IGenericRepository<TenantEntity> _tenantRepo;

    public GetAllTenantsHandler(IGenericRepository<TenantEntity> tenantRepo)
    {
        _tenantRepo = tenantRepo;
    }

    public async Task<List<TenantDto>> Handle(GetAllTenantsQuery request, CancellationToken ct)
    {
        var list = await _tenantRepo.GetAllAsync(x => !x.IsDeleted, ct);

        return list.Select(t => new TenantDto
        {
            Id = t.Id,
            Name = t.Name,
            Subdomain = t.Subdomain,
            LogoUrl = t.LogoUrl,
            Phone = t.Phone,
            Address = t.Address,
            IsActive = t.IsActive
        }).ToList();
    }
}
