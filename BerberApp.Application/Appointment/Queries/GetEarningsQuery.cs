using MediatR;

namespace BerberApp.Application.Appointment.Queries;

public class GetEarningsQuery : IRequest<EarningsDto>
{
    public Guid TenantId { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? StaffId { get; set; }
}

public class EarningsDto
{
    public decimal TotalEarnings { get; set; }
    public int TotalAppointments { get; set; }
    public decimal AveragePerAppointment { get; set; }

    public decimal TodayEarnings { get; set; }
    public int TodayAppointments { get; set; }

    public decimal WeekEarnings { get; set; }
    public int WeekAppointments { get; set; }

    public decimal MonthEarnings { get; set; }
    public int MonthAppointments { get; set; }


    public List<DailyEarningDto> Daily { get; set; } = new();
    public List<StaffEarningDto> ByStaff { get; set; } = new();
    public List<ServiceEarningDto> ByService { get; set; } = new();
}

public class DailyEarningDto
{
    public DateTime Date { get; set; }
    public decimal Earnings { get; set; }
    public int AppointmentCount { get; set; }
}

public class StaffEarningDto
{
    public string? StaffId { get; set; }
    public string? StaffName { get; set; }
    public decimal TotalEarnings { get; set; }
    public int AppointmentCount { get; set; }
    public decimal Average { get; set; }
}

public class ServiceEarningDto
{
    public string? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public decimal TotalEarnings { get; set; }
    public int AppointmentCount { get; set; }
    public decimal Price { get; set; }
}