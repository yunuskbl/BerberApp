using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class CompleteAppointmentHandler : IRequestHandler<CompleteAppointmentCommand, bool>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly INotificationService _notificationService;

    public CompleteAppointmentHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        INotificationService notificationService)
    {
        _appointmentRepo     = appointmentRepo;
        _customerRepo        = customerRepo;
        _serviceRepo         = serviceRepo;
        _notificationService = notificationService;
    }

    public async Task<bool> Handle(CompleteAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (appointment is null)
            throw new NotFoundException("Randevu", request.Id);

        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new BadRequestException("İptal edilmiş randevu tamamlanamaz.");

        if (appointment.Status == AppointmentStatus.Completed)
            throw new BadRequestException("Randevu zaten tamamlanmış.");

        appointment.Status = AppointmentStatus.Completed;
        await _appointmentRepo.UpdateAsync(appointment, ct);

        // Müşteriye değerlendirme bildirimi gönder
        if (!string.IsNullOrWhiteSpace(request.ReviewUrl))
        {
            var customer = await _customerRepo.GetByIdAsync(appointment.CustomerId, ct);
            var service  = await _serviceRepo.GetByIdAsync(appointment.ServiceId, ct);

            if (customer is not null && service is not null)
            {
                await _notificationService.SendAppointmentCompletedAsync(
                    customer.Phone,
                    new AppointmentStatusDto
                    {
                        Id           = appointment.Id,
                        TenantId     = appointment.TenantId,
                        CustomerName = customer.FullName,
                        ServiceName  = service.Name,
                        StartTime    = appointment.StartTime,
                        EndTime      = appointment.EndTime,
                        Status       = appointment.Status.ToString()
                    },
                    request.ReviewUrl);
            }
        }

        return true;
    }
}
