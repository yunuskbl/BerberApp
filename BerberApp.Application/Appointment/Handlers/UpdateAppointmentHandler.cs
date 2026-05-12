using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Appointment.Handlers;

public class UpdateAppointmentHandler : IRequestHandler<UpdateAppointmentCommand, AppointmentDto>
{
    private readonly IAppDbContext _context;
    private readonly INotificationService _notificationService;

    public UpdateAppointmentHandler(IAppDbContext context, INotificationService notificationService)
    {
        _context             = context;
        _notificationService = notificationService;
    }

    public async Task<AppointmentDto> Handle(UpdateAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _context.Appointments
            .Include(x => x.Customer)
            .Include(x => x.Staff)
            .Include(x => x.Service)
            .FirstOrDefaultAsync(x => x.Id == request.Id && x.TenantId == request.TenantId, ct)
            ?? throw new Exception("Randevu bulunamadı.");

        var service = await _context.Services
            .FirstOrDefaultAsync(x => x.Id == request.ServiceId && x.TenantId == request.TenantId, ct)
            ?? throw new Exception("Hizmet bulunamadı.");

        var staff = await _context.Staff
            .FirstOrDefaultAsync(x => x.Id == request.StaffId && x.TenantId == request.TenantId, ct)
            ?? throw new Exception("Personel bulunamadı.");

        appointment.StaffId   = request.StaffId;
        appointment.ServiceId = request.ServiceId;
        appointment.StartTime = request.StartTime;
        appointment.EndTime   = request.StartTime.AddMinutes(service.DurationMinutes);
        if (request.Notes is not null) appointment.Notes = request.Notes;

        await _context.SaveChangesAsync(ct);

        // Müşteriye bildirim gönder (arka planda, hata fırlatmasın)
        _ = _notificationService.SendAppointmentUpdatedAsync(
            appointment.Customer.Phone,
            new AppointmentStatusDto
            {
                Id           = appointment.Id,
                TenantId     = appointment.TenantId,
                CustomerName = appointment.Customer.FullName,
                ServiceName  = service.Name,
                StaffName    = staff.FullName,
                StartTime    = appointment.StartTime,
                EndTime      = appointment.EndTime,
                Status       = appointment.Status.ToString(),
            });

        return new AppointmentDto
        {
            Id              = appointment.Id,
            CustomerId      = appointment.CustomerId,
            CustomerName    = appointment.Customer.FullName,
            StaffId         = appointment.StaffId,
            StaffName       = staff.FullName,
            ServiceId       = appointment.ServiceId,
            ServiceName     = service.Name,
            StartTime       = appointment.StartTime,
            EndTime         = appointment.EndTime,
            Status          = appointment.Status,
            Notes           = appointment.Notes,
            DurationMinutes = service.DurationMinutes,
        };
    }
}
