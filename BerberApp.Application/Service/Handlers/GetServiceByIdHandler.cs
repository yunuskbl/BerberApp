using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Service.DTOs;
using BerberApp.Application.Service.Queries;
using MediatR;

namespace BerberApp.Application.Service.Handlers;

public class GetServiceByIdHandler : IRequestHandler<GetServiceByIdQuery, ServiceDto>
{
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;

    public GetServiceByIdHandler(IGenericRepository<ServiceEntity> serviceRepo)
    {
        _serviceRepo = serviceRepo;
    }

    public async Task<ServiceDto> Handle(GetServiceByIdQuery request, CancellationToken ct)
    {
        var service = await _serviceRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (service is null)
            throw new NotFoundException("Hizmet", request.Id);

        return new ServiceDto
        {
            Id = service.Id,
            Name = service.Name,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Currency = service.Currency,
            Color = service.Color,
            IsActive = service.IsActive
        };
    }
}