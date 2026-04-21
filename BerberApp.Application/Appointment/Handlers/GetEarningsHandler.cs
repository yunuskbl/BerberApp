using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class GetEarningsHandler : IRequestHandler<GetEarningsQuery, EarningsDto>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public GetEarningsHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<StaffEntity> staffRepo)
    {
        _appointmentRepo = appointmentRepo;
        _serviceRepo = serviceRepo;
        _staffRepo = staffRepo;
    }

    public async Task<EarningsDto> Handle(GetEarningsQuery request, CancellationToken ct)
    {
        var turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
        var todayEnd = todayStart.AddDays(1);
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.StartDate, DateTimeKind.Unspecified), turkeyTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.EndDate.AddDays(1), DateTimeKind.Unspecified), turkeyTz);

        var query = new List<AppointmentEntity>();
        var allAppointments = await _appointmentRepo.GetAllAsync(
            x => x.TenantId == request.TenantId &&
                 x.Status == AppointmentStatus.Completed, ct);

        var appointments = allAppointments
            .Where(x => x.StartTime >= startUtc && x.StartTime < endUtc)
            .ToList();

        if (request.StaffId.HasValue)
            appointments = appointments.Where(x => x.StaffId == request.StaffId.Value).ToList();

        var today = appointments
            .Where(x => x.StartTime >= TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(todayStart, DateTimeKind.Unspecified), turkeyTz) &&
                x.StartTime < TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(todayEnd, DateTimeKind.Unspecified), turkeyTz))
            .ToList();

        var thisWeek = appointments
            .Where(x => x.StartTime >= TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(weekStart, DateTimeKind.Unspecified), turkeyTz) &&
                x.StartTime < TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(weekStart.AddDays(7), DateTimeKind.Unspecified), turkeyTz))
            .ToList();

        var thisMonth = appointments
            .Where(x => x.StartTime >= TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(monthStart, DateTimeKind.Unspecified), turkeyTz) &&
                x.StartTime < TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(monthStart.AddMonths(1), DateTimeKind.Unspecified), turkeyTz))
            .ToList();

        var services = await _serviceRepo.GetAllAsync(x => x.TenantId == request.TenantId, ct);
        var staff = await _staffRepo.GetAllAsync(x => x.TenantId == request.TenantId, ct);

        var totalEarnings = appointments.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0);
        var todayEarnings = today.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0);
        var weekEarnings = thisWeek.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0);
        var monthEarnings = thisMonth.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0);

        var daily = appointments
            .GroupBy(x => new DateTime(x.StartTime.Year, x.StartTime.Month, x.StartTime.Day))
            .OrderBy(g => g.Key)
            .Select(g => new DailyEarningDto
            {
                Date = g.Key,
                Earnings = g.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0),
                AppointmentCount = g.Count()
            })
            .ToList();

        var byStaff = appointments
            .GroupBy(x => x.StaffId)
            .Select(g => new StaffEarningDto
            {
                StaffId = g.Key.ToString(),
                StaffName = staff.FirstOrDefault(s => s.Id == g.Key)?.FullName ?? "Unknown",
                TotalEarnings = g.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0),
                AppointmentCount = g.Count(),
                Average = g.Count() > 0 ?
                    g.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0) / g.Count() : 0
            })
            .ToList();

        var byService = appointments
            .GroupBy(x => x.ServiceId)
            .Select(g => new ServiceEarningDto
            {
                ServiceId = g.Key.ToString(),
                ServiceName = services.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Unknown",
                Price = services.FirstOrDefault(s => s.Id == g.Key)?.Price ?? 0,
                TotalEarnings = g.Sum(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Price ?? 0),
                AppointmentCount = g.Count()
            })
            .ToList();

        return new EarningsDto
        {
            TotalEarnings = totalEarnings,
            TotalAppointments = appointments.Count,
            AveragePerAppointment = appointments.Count > 0 ? totalEarnings / appointments.Count : 0,
            TodayEarnings = todayEarnings,
            TodayAppointments = today.Count,
            WeekEarnings = weekEarnings,
            WeekAppointments = thisWeek.Count,
            MonthEarnings = monthEarnings,
            MonthAppointments = thisMonth.Count,
            Daily = daily,
            ByStaff = byStaff,
            ByService = byService
        };
    }
}