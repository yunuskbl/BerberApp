using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Service.Commands;
using MediatR;

namespace BerberApp.Application.Service.Handlers;

public class DeleteServiceHandler : IRequestHandler<DeleteServiceCommand, bool>
{
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;

    public DeleteServiceHandler(IGenericRepository<ServiceEntity> serviceRepo)
    {
        _serviceRepo = serviceRepo;
    }

    public async Task<bool> Handle(DeleteServiceCommand request, CancellationToken ct)
    {
        var service = await _serviceRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (service is null)
            throw new NotFoundException("Hizmet", request.Id);

        await _serviceRepo.DeleteAsync(service, ct);
        return true;
    }
}