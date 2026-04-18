using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Service.Commands;
using BerberApp.Application.Service.DTOs;
using MediatR;

namespace BerberApp.Application.Service.Handlers;

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;

    public UpdateServiceHandler(IGenericRepository<ServiceEntity> serviceRepo)
    {
        _serviceRepo = serviceRepo;
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken ct)
    {
        var service = await _serviceRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (service is null)
            throw new NotFoundException("Hizmet", request.Id);

        service.Name = request.Name;
        service.DurationMinutes = request.DurationMinutes;
        service.Price = request.Price;
        service.Currency = request.Currency;
        service.Color = request.Color;
        service.IsActive = request.IsActive;

        await _serviceRepo.UpdateAsync(service, ct);

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