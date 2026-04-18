using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class CreateAppointmentHandler : IRequestHandler<CreateAppointmentCommand, AppointmentDto>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<WorkingHourEntity> _workingHourRepo;
    private readonly IWhatsAppService _whatsAppService;

    public CreateAppointmentHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<StaffEntity> staffRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<WorkingHourEntity> workingHourRepo,
        IWhatsAppService whatsAppService)
    {
        _appointmentRepo = appointmentRepo;
        _serviceRepo = serviceRepo;
        _staffRepo = staffRepo;
        _customerRepo = customerRepo;
        _workingHourRepo = workingHourRepo;
        _whatsAppService = whatsAppService;
    }

    public async Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken ct)
    {
        // Servis kontrolü
        var service = await _serviceRepo.GetAsync(
            x => x.Id == request.ServiceId && x.TenantId == request.TenantId, ct);
        if (service is null)
            throw new NotFoundException("Hizmet", request.ServiceId);

        // Personel kontrolü
        var staff = await _staffRepo.GetAsync(
            x => x.Id == request.StaffId && x.TenantId == request.TenantId, ct);
        if (staff is null)
            throw new NotFoundException("Personel", request.StaffId);

        // Müşteri kontrolü
        var customer = await _customerRepo.GetAsync(
            x => x.Id == request.CustomerId && x.TenantId == request.TenantId, ct);
        if (customer is null)
            throw new NotFoundException("Müşteri", request.CustomerId);

        // UTC'ye çevir
        var startTimeUtc = request.StartTime.ToUtc();
        var endTimeUtc = request.StartTime.AddMinutes(service.DurationMinutes).ToUtc();

        // Çalışma saati kontrolü
        var workingHour = await _workingHourRepo.GetAsync(
            x => x.StaffId == request.StaffId &&
                 x.DayOfWeek == request.StartTime.DayOfWeek &&
                 !x.IsOff, ct);

        if (workingHour is null)
            throw new BadRequestException("Personel bu gün çalışmıyor.");

        if (startTimeUtc.TimeOfDay < workingHour.StartTime.ToTimeSpan() ||
            endTimeUtc.TimeOfDay > workingHour.EndTime.ToTimeSpan())
            throw new BadRequestException("Seçilen saat çalışma saatleri dışında.");

        // Çakışma kontrolü
        var conflict = await _appointmentRepo.AnyAsync(
            x => x.StaffId == request.StaffId &&
                 x.TenantId == request.TenantId &&
                 x.Status != AppointmentStatus.Cancelled &&
                 x.StartTime < endTimeUtc &&
                 x.EndTime > startTimeUtc, ct);

        if (conflict)
            throw new ConflictException("Bu saatte başka bir randevu mevcut.");

        // Randevu oluştur
        var appointment = new AppointmentEntity
        {
            TenantId = request.TenantId,
            CustomerId = request.CustomerId,
            StaffId = request.StaffId,
            ServiceId = request.ServiceId,
            StartTime = startTimeUtc,
            EndTime = endTimeUtc,
            Status = AppointmentStatus.Confirmed,
            Notes = request.Notes
        };

        await _appointmentRepo.AddAsync(appointment, ct);

        // Müşteri ziyaret sayısını artır
        customer.TotalVisits++;
        await _customerRepo.UpdateAsync(customer, ct);
        try
        {
            await _whatsAppService.SendAppointmentConfirmedAsync(
                customer.Phone,
                customer.FullName,
                service.Name,
                staff.FullName,
                appointment.StartTime
            );
        }
        catch { /* Bildirim hatası randevuyu etkilemesin */ }
        return new AppointmentDto
        {
            Id = appointment.Id,
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            StaffId = staff.Id,
            StaffName = staff.FullName,
            ServiceId = service.Id,
            ServiceName = service.Name,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status,
            Notes = appointment.Notes,
            DurationMinutes = service.DurationMinutes
        };


    }
}