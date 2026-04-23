using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Appointment.Commands;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class CancelAppointmentHandler : IRequestHandler<CancelAppointmentCommand, bool>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IWhatsAppService _whatsAppService;

    public CancelAppointmentHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IWhatsAppService whatsAppService)
    {
        _appointmentRepo = appointmentRepo;
        _customerRepo = customerRepo;
        _whatsAppService = whatsAppService;
    }

    public async Task<bool> Handle(CancelAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (appointment is null)
            throw new NotFoundException("Randevu", request.Id);

        if (appointment.Status == AppointmentStatus.Completed)
            throw new BadRequestException("Tamamlanmış randevu iptal edilemez.");

        if (appointment.Status == AppointmentStatus.Cancelled)
            throw new BadRequestException("Randevu zaten iptal edilmiş.");

        var wasPending = appointment.Status == AppointmentStatus.Pending;
        appointment.Status = AppointmentStatus.Cancelled;
        await _appointmentRepo.UpdateAsync(appointment, ct);
        if (wasPending)
        {
            var customerToUpdate = await _customerRepo.GetByIdAsync(appointment.CustomerId, ct);
            if (customerToUpdate is not null && customerToUpdate.TotalVisits > 0)
            {
                customerToUpdate.TotalVisits--;
                await _customerRepo.UpdateAsync(customerToUpdate, ct);
            }
        }
        // WhatsApp bildirimi
        try
        {
            var customer = await _customerRepo.GetByIdAsync(appointment.CustomerId, ct);
            if (customer is not null)
            {
                await _whatsAppService.SendAppointmentCancelledAsync(
                    customer.Phone,
                    customer.FullName,
                    appointment.StartTime
                );
            }
        }
        catch { }

        return true;
    }
}