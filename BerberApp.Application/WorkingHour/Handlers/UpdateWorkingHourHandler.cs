using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.WorkingHour.Commands;
using BerberApp.Application.WorkingHour.DTOs;
using MediatR;

namespace BerberApp.Application.WorkingHour.Handlers;

public class UpdateWorkingHourHandler : IRequestHandler<UpdateWorkingHourCommand, WorkingHourDto>
{
    private readonly IGenericRepository<WorkingHourEntity> _workingHourRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public UpdateWorkingHourHandler(
        IGenericRepository<WorkingHourEntity> workingHourRepo,
        IGenericRepository<StaffEntity> staffRepo)
    {
        _workingHourRepo = workingHourRepo;
        _staffRepo = staffRepo;
    }

    public async Task<WorkingHourDto> Handle(UpdateWorkingHourCommand request, CancellationToken ct)
    {
        var staffExists = await _staffRepo.AnyAsync(
            x => x.Id == request.StaffId && x.TenantId == request.TenantId, ct);

        if (!staffExists)
            throw new NotFoundException("Personel", request.StaffId);

        var workingHour = await _workingHourRepo.GetAsync(
            x => x.Id == request.Id && x.StaffId == request.StaffId, ct);

        if (workingHour is null)
            throw new NotFoundException("Çalışma saati", request.Id);

        workingHour.DayOfWeek = request.DayOfWeek;
        workingHour.StartTime = request.StartTime;
        workingHour.EndTime = request.EndTime;
        workingHour.IsOff = request.IsOff;

        await _workingHourRepo.UpdateAsync(workingHour, ct);

        return new WorkingHourDto
        {
            Id = workingHour.Id,
            StaffId = workingHour.StaffId,
            DayOfWeek = workingHour.DayOfWeek,
            StartTime = workingHour.StartTime,
            EndTime = workingHour.EndTime,
            IsOff = workingHour.IsOff
        };
    }
}