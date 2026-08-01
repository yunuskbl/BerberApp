using BerberApp.Application.Appointment.Commands;
using BerberApp.Application.Appointment.DTOs;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Appointment.Handlers;

public class CompleteAppointmentHandler : IRequestHandler<CompleteAppointmentCommand, CompleteAppointmentResult>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<AppointmentActualServiceEntity> _actualServiceRepo;
    private readonly IGenericRepository<PriceDifferenceEntity> _priceDiffRepo;
    private readonly INotificationService _notificationService;
    private readonly IAppDbContext _context;

    public CompleteAppointmentHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<AppointmentActualServiceEntity> actualServiceRepo,
        IGenericRepository<PriceDifferenceEntity> priceDiffRepo,
        INotificationService notificationService,
        IAppDbContext context)
    {
        _appointmentRepo     = appointmentRepo;
        _customerRepo        = customerRepo;
        _serviceRepo         = serviceRepo;
        _actualServiceRepo   = actualServiceRepo;
        _priceDiffRepo       = priceDiffRepo;
        _notificationService = notificationService;
        _context             = context;
    }

    public async Task<CompleteAppointmentResult> Handle(CompleteAppointmentCommand request, CancellationToken ct)
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

        // Gerçekte yapılan hizmetleri kaydet ve fiş kalemleri için topla
        var receiptItems = new List<(string Name, decimal Price)>();

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

            receiptItems.Add((svc.Name, svc.Price ?? 0));
        }

        // Seçilen hizmet yoksa orijinal hizmeti kullan
        if (!receiptItems.Any())
        {
            var originalSvc = await _serviceRepo.GetByIdAsync(appointment.ServiceId, ct);
            receiptItems.Add((originalSvc?.Name ?? "Hizmet", request.ActualTotalPrice ?? originalSvc?.Price ?? 0));
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

        // ── Otomatik fiş oluştur ──────────────────────────────
        var receiptNumber = await GenerateReceiptNumberAsync(appointment.TenantId, now.Year, ct);
        var totalAmount   = request.ActualTotalPrice ?? receiptItems.Sum(x => x.Price);

        // Fişin para birimi: istemci belirtmişse onu, yoksa orijinal hizmetin
        // para birimini kullan. Belirtilmeden varsayılana ("TRY") düşülmesi,
        // USD/farklı para birimli hizmetleri sessizce yanlış etiketliyordu.
        var receiptCurrency = request.ActualCurrency;
        if (string.IsNullOrWhiteSpace(receiptCurrency))
        {
            var originalSvcForCurrency = await _serviceRepo.GetByIdAsync(appointment.ServiceId, ct);
            receiptCurrency = originalSvcForCurrency?.Currency ?? "TRY";
        }

        // Tek kalem varsa ActualTotalPrice'ı direkt kullan
        var items = receiptItems.Select((x, idx) =>
        {
            var price = receiptItems.Count == 1
                ? totalAmount
                : x.Price;
            return new ReceiptItemEntity { ServiceName = x.Name, Quantity = 1, UnitPrice = price };
        }).ToList();

        var receipt = new ReceiptEntity
        {
            TenantId      = appointment.TenantId,
            CustomerId    = appointment.CustomerId,
            AppointmentId = appointment.Id,
            ReceiptNumber = receiptNumber,
            TotalAmount   = totalAmount,
            Currency      = receiptCurrency,
            Items         = items,
        };

        _context.Receipts.Add(receipt);
        await _context.SaveChangesAsync(ct);

        // ── Müşteriye değerlendirme bildirimi gönder ──────────
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

        return new CompleteAppointmentResult { ReceiptNumber = receiptNumber };
    }

    private async Task<string> GenerateReceiptNumberAsync(Guid tenantId, int year, CancellationToken ct)
    {
        var lastNumber = await _context.Receipts
            .Where(r => r.TenantId == tenantId && r.ReceiptNumber.StartsWith(year + "-"))
            .OrderByDescending(r => r.ReceiptNumber)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(ct);

        int seq = 1;
        if (lastNumber is not null)
        {
            var parts = lastNumber.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[1], out int last))
                seq = last + 1;
        }

        return $"{year}-{seq:D4}";
    }
}
