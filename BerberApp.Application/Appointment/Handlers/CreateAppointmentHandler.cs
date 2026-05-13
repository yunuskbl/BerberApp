using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
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
    private readonly IGenericRepository<SubscriptionEntity> _subscriptionRepo;
    private readonly IWhatsAppService _whatsAppService;
    private readonly INotificationService _notificationService;

    public CreateAppointmentHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<StaffEntity> staffRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<WorkingHourEntity> workingHourRepo,
        IGenericRepository<SubscriptionEntity> subscriptionRepo,
        IWhatsAppService whatsAppService,
        INotificationService notificationService)
    {
        _appointmentRepo = appointmentRepo;
        _serviceRepo = serviceRepo;
        _staffRepo = staffRepo;
        _customerRepo = customerRepo;
        _workingHourRepo = workingHourRepo;
        _subscriptionRepo = subscriptionRepo;
        _whatsAppService = whatsAppService;
        _notificationService = notificationService;
    }

    public async Task<AppointmentDto> Handle(CreateAppointmentCommand request, CancellationToken ct)
    {
        // Aylık randevu plan limiti kontrolü
        var subscription = await _subscriptionRepo.GetAsync(
            x => x.TenantId == request.TenantId && x.Status == SubscriptionStatus.Active, ct);
        var plan = subscription?.Plan ?? PlanType.Baslangic;

        int appointmentLimit = plan switch
        {
            PlanType.Baslangic   => 100,
            PlanType.Profesyonel => 500,
            _                    => int.MaxValue
        };

        if (appointmentLimit < int.MaxValue)
        {
            var now = DateTime.UtcNow;
            var monthStart = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);
            var monthEnd = monthStart.AddMonths(1);

            int monthlyCount = await _appointmentRepo.CountAsync(
                x => x.TenantId == request.TenantId &&
                     x.StartTime >= monthStart &&
                     x.StartTime < monthEnd &&
                     x.Status != AppointmentStatus.Cancelled, ct);

            if (monthlyCount >= appointmentLimit)
                throw new BadRequestException(
                    $"Bu ay için randevu limitinize ulaştınız ({appointmentLimit} randevu). " +
                    "Daha fazla randevu için planınızı yükseltin.");
        }

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

        // request.StartTime zaten UTC geliyor, ToUtc() ÇAĞIRMA
        var startTimeUtc = DateTime.SpecifyKind(request.StartTime, DateTimeKind.Utc);
        var durationMinutes = request.TotalDurationMinutes ?? service.DurationMinutes;
        var endTimeUtc = startTimeUtc.AddMinutes(durationMinutes);

        // Geçmiş tarih kontrolü
        if (startTimeUtc <= DateTime.UtcNow)
            throw new BadRequestException("Randevu saati geçmiş bir tarih olamaz.");

        // Türkiye saatine çevir — çalışma saati karşılaştırması için
        var turkeyTz = GetTurkeyTimeZone();
        var startTimeTurkey = TimeZoneInfo.ConvertTimeFromUtc(startTimeUtc, turkeyTz);
        var endTimeTurkey = TimeZoneInfo.ConvertTimeFromUtc(endTimeUtc, turkeyTz);

        // Çalışma saati kontrolü — Türkiye saatiyle karşılaştır
        var workingHour = await _workingHourRepo.GetAsync(
            x => x.StaffId == request.StaffId &&
                 x.DayOfWeek == startTimeTurkey.DayOfWeek &&
                 !x.IsOff, ct);

        if (workingHour is null)
            throw new BadRequestException("Personel bu gün çalışmıyor.");

        if (startTimeTurkey.TimeOfDay < workingHour.StartTime.ToTimeSpan() ||
            endTimeTurkey.TimeOfDay > workingHour.EndTime.ToTimeSpan())
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

        // Aynı müşteri aynı gün max 1 randevu (Türkiye günü bazında)
        var turkeyDayStart = new DateTime(startTimeTurkey.Year, startTimeTurkey.Month, startTimeTurkey.Day, 0, 0, 0);
        var turkeyDayEnd = turkeyDayStart.AddDays(1);
        var dayStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(turkeyDayStart, DateTimeKind.Unspecified), turkeyTz);
        var dayEndUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(turkeyDayEnd, DateTimeKind.Unspecified), turkeyTz);

        var dailyCount = await _appointmentRepo.AnyAsync(
            x => x.CustomerId == request.CustomerId &&
                 x.TenantId == request.TenantId &&
                 x.StartTime >= dayStartUtc &&
                 x.StartTime < dayEndUtc &&
                 x.Status != AppointmentStatus.Cancelled, ct);

        if (dailyCount)
            throw new BadRequestException("Aynı gün için birden fazla randevu oluşturamazsınız.");

        // Onay bekleyen randevusu olan müşteri tekrar alamasın
        var hasPending = await _appointmentRepo.AnyAsync(
            x => x.CustomerId == request.CustomerId &&
                 x.TenantId == request.TenantId &&
                 x.Status == AppointmentStatus.Pending, ct);

        if (hasPending)
            throw new BadRequestException("Onay bekleyen bir randevunuz bulunuyor. Yeni randevu alamazsınız.");

        // Randevu oluştur
        var appointment = new AppointmentEntity
        {
            TenantId = request.TenantId,
            CustomerId = request.CustomerId,
            StaffId = request.StaffId,
            ServiceId = request.ServiceId,
            StartTime = startTimeUtc,
            EndTime = endTimeUtc,
            Status = request.IsFromBookingPage
                  ? AppointmentStatus.Pending
                  : AppointmentStatus.Confirmed,
            Notes = request.Notes
        };

        await _appointmentRepo.AddAsync(appointment, ct);

        customer.TotalVisits++;
        await _customerRepo.UpdateAsync(customer, ct);

        if (request.IsFromBookingPage)
        {
            var notificationPhone = request.NotificationPhone ?? staff.Phone;
            Console.WriteLine($"[WHATSAPP] Bildirim numarası: '{notificationPhone}'");

            if (!string.IsNullOrWhiteSpace(notificationPhone))
            {
                try
                {
                    var pendingList = await _appointmentRepo.GetAllAsync(
                        x => x.TenantId == request.TenantId && x.Status == AppointmentStatus.Pending, ct);
                    var sequenceNumber = pendingList.OrderBy(x => x.StartTime).ToList()
                        .FindIndex(x => x.Id == appointment.Id) + 1;
                    if (sequenceNumber == 0) sequenceNumber = pendingList.Count;
                    await _whatsAppService.SendNewAppointmentRequestAsync(
                        notificationPhone,
                        customer.FullName,
                        customer.Phone,
                        request.ServiceNamesDisplay ?? service.Name,
                        startTimeTurkey,
                        sequenceNumber
                    );
                    Console.WriteLine($"[WHATSAPP] Bildirim gönderildi: {notificationPhone}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WHATSAPP HATA] {ex.Message}");
                }
            }
        }
        else
        {
            // İşletme sahibi randevu oluşturdu → müşteriye onay bildirimi gönder
            await _notificationService.SendAppointmentConfirmedAsync(
                customer.Phone,
                new AppointmentStatusDto
                {
                    Id = appointment.Id,
                    TenantId = request.TenantId,
                    CustomerName = customer.FullName,
                    ServiceName = request.ServiceNamesDisplay ?? service.Name,
                    StaffName = staff.FullName,
                    StartTime = appointment.StartTime,
                    EndTime = appointment.EndTime,
                    Status = appointment.Status.ToString()
                });
        }

        return new AppointmentDto
        {
            Id = appointment.Id,
            CustomerId = customer.Id,
            CustomerName = customer.FullName,
            StaffId = staff.Id,
            StaffName = staff.FullName,
            ServiceId = service.Id,
            ServiceName = request.ServiceNamesDisplay ?? service.Name,
            StartTime = appointment.StartTime,
            EndTime = appointment.EndTime,
            Status = appointment.Status,
            Notes = appointment.Notes,
            DurationMinutes = durationMinutes
        };
    }

    private static TimeZoneInfo GetTurkeyTimeZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time"); }
        catch { return TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul"); }
    }
}