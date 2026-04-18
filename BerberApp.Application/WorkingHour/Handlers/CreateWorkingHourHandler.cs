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

public class CreateWorkingHourHandler : IRequestHandler<CreateWorkingHourCommand, WorkingHourDto>
{
    private readonly IGenericRepository<WorkingHourEntity> _workingHourRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public CreateWorkingHourHandler(
        IGenericRepository<WorkingHourEntity> workingHourRepo,
        IGenericRepository<StaffEntity> staffRepo)
    {
        _workingHourRepo = workingHourRepo;
        _staffRepo = staffRepo;
    }

    public async Task<WorkingHourDto> Handle(CreateWorkingHourCommand request, CancellationToken ct)
    {
        var staffExists = await _staffRepo.AnyAsync(
            x => x.Id == request.StaffId && x.TenantId == request.TenantId, ct);

        if (!staffExists)
            throw new NotFoundException("Personel", request.StaffId);

        var exists = await _workingHourRepo.AnyAsync(
            x => x.StaffId == request.StaffId && x.DayOfWeek == request.DayOfWeek, ct);

        if (exists)
            throw new ConflictException("Bu gün için çalışma saati zaten tanımlı.");

        var workingHour = new WorkingHourEntity
        {
            StaffId = request.StaffId,
            DayOfWeek = request.DayOfWeek,
            StartTime = request.StartTime,
            EndTime = request.EndTime,
            IsOff = request.IsOff
        };

        await _workingHourRepo.AddAsync(workingHour, ct);
        return ToDto(workingHour);
    }

    private static WorkingHourDto ToDto(WorkingHourEntity w) => new()
    {
        Id = w.Id,
        StaffId = w.StaffId,
        DayOfWeek = w.DayOfWeek,
        StartTime = w.StartTime,
        EndTime = w.EndTime,
        IsOff = w.IsOff
    };
}