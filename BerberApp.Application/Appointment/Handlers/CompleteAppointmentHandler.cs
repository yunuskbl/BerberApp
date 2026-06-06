using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class CompleteAppointmentHandler : IRequestHandler<CompleteAppointmentCommand, bool>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<AppointmentActualServiceEntity> _actualServiceRepo;
    private readonly IGenericRepository<PriceDifferenceEntity> _priceDiffRepo;
    private readonly INotificationService _notificationService;

    public CompleteAppointmentHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<AppointmentActualServiceEntity> actualServiceRepo,
        IGenericRepository<PriceDifferenceEntity> priceDiffRepo,
        INotificationService notificationService)
    {
        _appointmentRepo     = appointmentRepo;
        _customerRepo        = customerRepo;
        _serviceRepo         = serviceRepo;
        _actualServiceRepo   = actualServiceRepo;
        _priceDiffRepo       = priceDiffRepo;
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

        var now = DateTime.UtcNow;

        appointment.Status           = AppointmentStatus.Completed;
        appointment.CompletedAt      = now;
        appointment.ActualTotalPrice = request.ActualTotalPrice;
        appointment.CompletionNotes  = request.CompletionNotes;

        await _appointmentRepo.UpdateAsync(appointment, ct);

        // Gerçekte yapılan hizmetleri kaydet
        foreach (var serviceId in request.ActualServiceIds)
        {
            var svc = await _serviceRepo.GetByIdAsync(serviceId, ct);
            if (svc is null) continue;

            await _actualServiceRepo.AddAsync(new AppointmentActualService
            {
                AppointmentId = appointment.Id,
                ServiceId     = serviceId,
                Price         = svc.Price ?? 0
            }, ct);
        }

        // Fiyat farkı kaydı
        if (request.ActualTotalPrice.HasValue)
        {
            var originalService = await _serviceRepo.GetByIdAsync(appointment.ServiceId, ct);
            var originalPrice   = originalService?.Price ?? 0;
            var actualPrice     = request.ActualTotalPrice.Value;
            var diff            = actualPrice - originalPrice;

            if (diff != 0)
            {
                await _priceDiffRepo.AddAsync(new PriceDifference
                {
                    TenantId      = appointment.TenantId,
                    AppointmentId = appointment.Id,
                    OriginalPrice = originalPrice,
                    ActualPrice   = actualPrice,
                    Difference    = diff,
                    CompletedAt   = now
                }, ct);
            }
        }

        // Müşteriye değerlendirme bildirimi gönder
        if (!string.IsNullOrWhiteSpace(request.ReviewUrl))
        {
            var customer = await _customerRepo.GetByIdAsync(appointment.CustomerId, ct);
            var service  = await _serviceRepo.GetByIdAsync(appointment.ServiceId, ct);

            if (customer is not null && service is not null)
            {
                try
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
                catch { /* Bildirim hatası tamamlamayı engellemesin */ }
            }
        }

        return true;
    }
}
