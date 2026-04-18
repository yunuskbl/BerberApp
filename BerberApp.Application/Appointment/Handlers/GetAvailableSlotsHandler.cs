using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Appointment.Queries;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class GetAvailableSlotsHandler : IRequestHandler<GetAvailableSlotsQuery, List<AvailableSlotDto>>
{
    private readonly IGenericRepository<WorkingHourEntity> _workingHourRepo;
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public GetAvailableSlotsHandler(
        IGenericRepository<WorkingHourEntity> workingHourRepo,
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<StaffEntity> staffRepo)
    {
        _workingHourRepo = workingHourRepo;
        _appointmentRepo = appointmentRepo;
        _serviceRepo = serviceRepo;
        _staffRepo = staffRepo;
    }

    public async Task<List<AvailableSlotDto>> Handle(GetAvailableSlotsQuery request, CancellationToken ct)
    {
        var service = await _serviceRepo.GetAsync(
            x => x.Id == request.ServiceId && x.TenantId == request.TenantId, ct);
        if (service is null)
            throw new NotFoundException("Hizmet", request.ServiceId);

        var staffExists = await _staffRepo.AnyAsync(
            x => x.Id == request.StaffId && x.TenantId == request.TenantId, ct);
        if (!staffExists)
            throw new NotFoundException("Personel", request.StaffId);

        var dayOfWeek = request.Date.DayOfWeek;
        var workingHour = await _workingHourRepo.GetAsync(
            x => x.StaffId == request.StaffId &&
                 x.DayOfWeek == dayOfWeek &&
                 !x.IsOff, ct);

        if (workingHour is null)
            return new List<AvailableSlotDto>();

        var dateStart = request.Date.Date.ToUtc();
        var dateEnd = dateStart.AddDays(1);

        var existingAppointments = await _appointmentRepo.GetAllAsync(
            x => x.StaffId == request.StaffId &&
                 x.TenantId == request.TenantId &&
                 x.StartTime >= dateStart &&
                 x.StartTime < dateEnd &&
                 x.Status != AppointmentStatus.Cancelled, ct);

        var slots = new List<AvailableSlotDto>();
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        var startTs = new TimeSpan(workingHour.StartTime.Hour, workingHour.StartTime.Minute, 0);
        var endTs = new TimeSpan(workingHour.EndTime.Hour, workingHour.EndTime.Minute, 0);
        var current = (request.Date.Date + startTs).ToUtc();
        var endOfDay = (request.Date.Date + endTs).ToUtc();

        while (current + duration <= endOfDay)
        {
            var slotEnd = current + duration;

            var isAvailable = !existingAppointments.Any(a =>
                a.StartTime < slotEnd && a.EndTime > current);

            slots.Add(new AvailableSlotDto
            {
                StartTime = current,
                EndTime = slotEnd,
                IsAvailable = isAvailable
            });

            current += duration;
        }

        return slots;
    }
}