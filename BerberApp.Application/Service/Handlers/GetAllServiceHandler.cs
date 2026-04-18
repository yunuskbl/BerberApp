using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Service.DTOs;
using BerberApp.Application.Service.Queries;
using MediatR;

namespace BerberApp.Application.Service.Handlers;

public class GetAllServicesHandler : IRequestHandler<GetAllServicesQuery, List<ServiceDto>>
{
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;

    public GetAllServicesHandler(IGenericRepository<ServiceEntity> serviceRepo)
    {
        _serviceRepo = serviceRepo;
    }

    public async Task<List<ServiceDto>> Handle(GetAllServicesQuery request, CancellationToken ct)
    {
        var list = await _serviceRepo.GetAllAsync(x => x.TenantId == request.TenantId, ct);

        return list.Select(s => new ServiceDto
        {
            Id = s.Id,
            Name = s.Name,
            DurationMinutes = s.DurationMinutes,
            Price = s.Price,
            Currency = s.Currency,
            Color = s.Color,
            IsActive = s.IsActive
        }).ToList();
    }
}