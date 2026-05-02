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
    private readonly ITranslationService _translation;

    public CreateServiceHandler(
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<TenantEntity> tenantRepo,
        ITranslationService translation)
    {
        _serviceRepo = serviceRepo;
        _tenantRepo = tenantRepo;
        _translation = translation;
    }

    public async Task<ServiceDto> Handle(CreateServiceCommand request, CancellationToken ct)
    {
        var tenantExists = await _tenantRepo.AnyAsync(x => x.Id == request.TenantId, ct);
        if (!tenantExists)
            throw new NotFoundException("Tenant", request.TenantId);

        var (nameEn, nameRu) = await TranslateNameAsync(request.Name, ct);

        var service = new ServiceEntity
        {
            TenantId = request.TenantId,
            Name = request.Name,
            NameEn = nameEn,
            NameRu = nameRu,
            DurationMinutes = request.DurationMinutes,
            Price = request.Price,
            Currency = request.Currency,
            Color = request.Color,
            IsActive = true
        };

        await _serviceRepo.AddAsync(service, ct);
        return ToDto(service);
    }

    private async Task<(string? en, string? ru)> TranslateNameAsync(string name, CancellationToken ct)
    {
        var enTask = _translation.TranslateAsync(name, "en", ct);
        var ruTask = _translation.TranslateAsync(name, "ru", ct);
        await Task.WhenAll(enTask, ruTask);
        return (enTask.Result, ruTask.Result);
    }

    private static ServiceDto ToDto(ServiceEntity s) => new()
    {
        Id = s.Id,
        Name = s.Name,
        NameEn = s.NameEn,
        NameRu = s.NameRu,
        DurationMinutes = s.DurationMinutes,
        Price = s.Price,
        Currency = s.Currency,
        Color = s.Color,
        IsActive = s.IsActive
    };
}