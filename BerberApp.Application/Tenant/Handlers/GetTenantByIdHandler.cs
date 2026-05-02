using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.DTOs;
using BerberApp.Application.Tenant.Queries;
using MediatR;

namespace BerberApp.Application.Tenant.Handlers;

public class GetTenantByIdHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly IGenericRepository<TenantEntity> _tenantRepo;

    public GetTenantByIdHandler(IGenericRepository<TenantEntity> tenantRepo)
    {
        _tenantRepo = tenantRepo;
    }

    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(request.Id, ct);

        if (tenant is null)
            throw new NotFoundException("Tenant", request.Id);

        return new TenantDto
        {
            Id = tenant.Id,
            Name = tenant.Name,
            Subdomain = tenant.Subdomain,
            LogoUrl = tenant.LogoUrl,
            Phone = tenant.Phone,
            NotificationPhone = tenant.NotificationPhone,
            Address = tenant.Address,
            IsActive = tenant.IsActive,
            ThemeColor = tenant.ThemeColor
        };
    }
}
