using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Tenant.DTOs;
using BerberApp.Application.Tenant.Queries;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Tenant.Handlers;

public class GetTenantByIdHandler : IRequestHandler<GetTenantByIdQuery, TenantDto>
{
    private readonly IGenericRepository<TenantEntity> _tenantRepo;
    private readonly IAppDbContext _context;

    public GetTenantByIdHandler(IGenericRepository<TenantEntity> tenantRepo, IAppDbContext context)
    {
        _tenantRepo = tenantRepo;
        _context = context;
    }

    public async Task<TenantDto> Handle(GetTenantByIdQuery request, CancellationToken ct)
    {
        var tenant = await _tenantRepo.GetByIdAsync(request.Id, ct);

        if (tenant is null)
            throw new NotFoundException("Tenant", request.Id);

        var subscription = await _context.Subscriptions
            .Where(x => x.TenantId == request.Id
                     && x.Status == SubscriptionStatus.Active
                     && x.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(ct);

        var plan = subscription?.Plan ?? PlanType.Baslangic;

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
            ThemeColor = tenant.ThemeColor,
            PreferredNotificationChannel = tenant.PreferredNotificationChannel,
            PlanType = plan.ToString(),
            BusinessType = tenant.BusinessType,
        };
    }
}
