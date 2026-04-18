using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.WorkingHour.Commands;
using MediatR;

namespace BerberApp.Application.WorkingHour.Handlers;

public class DeleteWorkingHourHandler : IRequestHandler<DeleteWorkingHourCommand, bool>
{
    private readonly IGenericRepository<WorkingHourEntity> _workingHourRepo;

    public DeleteWorkingHourHandler(IGenericRepository<WorkingHourEntity> workingHourRepo)
    {
        _workingHourRepo = workingHourRepo;
    }

    public async Task<bool> Handle(DeleteWorkingHourCommand request, CancellationToken ct)
    {
        var workingHour = await _workingHourRepo.GetAsync(
            x => x.Id == request.Id, ct);

        if (workingHour is null)
            throw new NotFoundException("Çalışma saati", request.Id);

        await _workingHourRepo.DeleteAsync(workingHour, ct);
        return true;
    }
}