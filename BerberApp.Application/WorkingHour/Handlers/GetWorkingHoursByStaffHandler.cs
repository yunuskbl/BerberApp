using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.WorkingHour.DTOs;
using BerberApp.Application.WorkingHour.Queries;
using MediatR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BerberApp.Application.WorkingHour.Handlers;

public class GetWorkingHoursByStaffHandler : IRequestHandler<GetWorkingHoursByStaffQuery, List<WorkingHourDto>>
{
    private readonly IGenericRepository<WorkingHourEntity> _workingHourRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public GetWorkingHoursByStaffHandler(
        IGenericRepository<WorkingHourEntity> workingHourRepo,
        IGenericRepository<StaffEntity> staffRepo)
    {
        _workingHourRepo = workingHourRepo;
        _staffRepo = staffRepo;
    }

    public async Task<List<WorkingHourDto>> Handle(GetWorkingHoursByStaffQuery request, CancellationToken ct)
    {
        var staffExists = await _staffRepo.AnyAsync(
            x => x.Id == request.StaffId && x.TenantId == request.TenantId, ct);

        if (!staffExists)
            throw new NotFoundException("Personel", request.StaffId);

        var list = await _workingHourRepo.GetAllAsync(
            x => x.StaffId == request.StaffId, ct);

        return list
            .OrderBy(x => x.DayOfWeek)
            .Select(w => new WorkingHourDto
            {
                Id = w.Id,
                StaffId = w.StaffId,
                DayOfWeek = w.DayOfWeek,
                StartTime = w.StartTime,
                EndTime = w.EndTime,
                IsOff = w.IsOff
            }).ToList();
    }
}