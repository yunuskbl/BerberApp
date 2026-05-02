using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Service.Commands;
using BerberApp.Application.Service.DTOs;
using MediatR;

namespace BerberApp.Application.Service.Handlers;

public class UpdateServiceHandler : IRequestHandler<UpdateServiceCommand, ServiceDto>
{
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly ITranslationService _translation;

    public UpdateServiceHandler(IGenericRepository<ServiceEntity> serviceRepo, ITranslationService translation)
    {
        _serviceRepo = serviceRepo;
        _translation = translation;
    }

    public async Task<ServiceDto> Handle(UpdateServiceCommand request, CancellationToken ct)
    {
        var service = await _serviceRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (service is null)
            throw new NotFoundException("Hizmet", request.Id);

        // Ad değiştiyse yeniden çevir
        if (service.Name != request.Name)
        {
            var enTask = _translation.TranslateAsync(request.Name, "en", ct);
            var ruTask = _translation.TranslateAsync(request.Name, "ru", ct);
            await Task.WhenAll(enTask, ruTask);
            service.NameEn = enTask.Result;
            service.NameRu = ruTask.Result;
        }

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
            NameEn = service.NameEn,
            NameRu = service.NameRu,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Currency = service.Currency,
            Color = service.Color,
            IsActive = service.IsActive
        };
    }
}