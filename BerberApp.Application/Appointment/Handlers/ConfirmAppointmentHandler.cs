using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class ConfirmAppointmentHandler : IRequestHandler<ConfirmAppointmentCommand, bool>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;
    private readonly IWhatsAppService _whatsAppService;

    public ConfirmAppointmentHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<StaffEntity> staffRepo,
        IWhatsAppService whatsAppService)
    {
        _appointmentRepo = appointmentRepo;
        _customerRepo = customerRepo;
        _serviceRepo = serviceRepo;
        _staffRepo = staffRepo;
        _whatsAppService = whatsAppService;
    }

    public async Task<bool> Handle(ConfirmAppointmentCommand request, CancellationToken ct)
    {
        var appointment = await _appointmentRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (appointment is null)
            throw new NotFoundException("Randevu", request.Id);

        if (appointment.Status != AppointmentStatus.Pending)
            throw new BadRequestException("Sadece bekleyen randevular onaylanabilir.");

        appointment.Status = AppointmentStatus.Confirmed;
        await _appointmentRepo.UpdateAsync(appointment, ct);

        // WhatsApp onay bildirimi
        try
        {
            var customer = await _customerRepo.GetByIdAsync(appointment.CustomerId, ct);
            var service = await _serviceRepo.GetByIdAsync(appointment.ServiceId, ct);
            var staff = await _staffRepo.GetByIdAsync(appointment.StaffId, ct);

            if (customer is not null && service is not null && staff is not null)
            {
                await _whatsAppService.SendAppointmentConfirmedAsync(
                    customer.Phone,
                    customer.FullName,
                    service.Name,
                    staff.FullName,
                    appointment.StartTime
                );
            }
        }
        catch(Exception ex)
        {
            Console.WriteLine($"WhatsApp hata: {ex.Message}");
        }

        return true;
    }
}