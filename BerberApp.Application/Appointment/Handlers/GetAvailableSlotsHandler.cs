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

        // UTC → Türkiye saatine çevir
        var turkeyTz = GetTurkeyTimeZone();
        var turkeyDate = TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(request.Date, DateTimeKind.Utc), turkeyTz);

        var dayOfWeek = turkeyDate.DayOfWeek;
        var workingHour = await _workingHourRepo.GetAsync(
            x => x.StaffId == request.StaffId &&
                 x.DayOfWeek == dayOfWeek &&
                 !x.IsOff, ct);

        if (workingHour is null)
            return new List<AvailableSlotDto>();

        // Türkiye saatine göre günün başı ve sonu
        var turkeyDayStart = new DateTime(turkeyDate.Year, turkeyDate.Month, turkeyDate.Day,
            workingHour.StartTime.Hour, workingHour.StartTime.Minute, 0);
        var turkeyDayEnd = new DateTime(turkeyDate.Year, turkeyDate.Month, turkeyDate.Day,
            workingHour.EndTime.Hour, workingHour.EndTime.Minute, 0);

        // Gece yarısını geçerse ertesi güne al
        if (workingHour.EndTime.Hour == 0 && workingHour.EndTime.Minute == 0)
            turkeyDayEnd = turkeyDayEnd.AddDays(1);

        // UTC'ye çevir
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(turkeyDayStart, DateTimeKind.Unspecified), turkeyTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(turkeyDayEnd, DateTimeKind.Unspecified), turkeyTz);

        // Günün UTC aralığı
        var dateStartUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(
                new DateTime(turkeyDate.Year, turkeyDate.Month, turkeyDate.Day, 0, 0, 0),
                DateTimeKind.Unspecified), turkeyTz);
        var dateEndUtc = dateStartUtc.AddDays(1);

        var existingAppointments = await _appointmentRepo.GetAllAsync(
            x => x.StaffId == request.StaffId &&
                 x.TenantId == request.TenantId &&
                 x.StartTime >= dateStartUtc &&
                 x.StartTime < dateEndUtc &&
                 x.Status != AppointmentStatus.Cancelled, ct);

        var slots = new List<AvailableSlotDto>();
        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        var current = startUtc;

        while (current + duration <= endUtc)
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

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }
}