using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Service.Commands;
using BerberApp.Application.Service.DTOs;
using MediatR;

namespace BerberApp.Application.Service.Handlers;

public class CreateServiceHandler : IRequestHandler<CreateServiceCommand, ServiceDto>
{
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<TenantEntity> _tenantRepo;

    public CreateServiceHandler(
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<TenantEntity> tenantRepo)
    {
        _serviceRepo = serviceRepo;
        _tenantRepo = tenantRepo;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken ct)
    {
        var tenantExists = await _tenantRepo.AnyAsync(x => x.Id == request.TenantId, ct);
        if (!tenantExists)
            throw new NotFoundException("Tenant", request.TenantId);

        var service = new ServiceEntity
        {
            TenantId = request.TenantId,
            Name = request.Name,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Currency = request.Currency,
            Color = request.Color,
            IsActive = true
        };

        await _serviceRepo.AddAsync(service, ct);
        return ToDto(service);
    }

    private static ServiceDto ToDto(ServiceEntity s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        DurationMinutes = s.DurationMinutes,
        Price = s.Price,
        Currency = s.Currency,
        Color = s.Color,
        IsActive = s.IsActive
    };
}