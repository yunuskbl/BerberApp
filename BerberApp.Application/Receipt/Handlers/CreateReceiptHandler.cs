using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Receipt.Commands;
using BerberApp.Application.Receipt.Dtos;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Receipt.Handlers;

public class CreateReceiptHandler : IRequestHandler<CreateReceiptCommand, ReceiptDto>
{
    private readonly IAppDbContext _context;

    public CreateReceiptHandler(IAppDbContext context) => _context = context;

    /// <summary>Aynı anda kesilen fişler numarada çakışırsa kaç kez yeniden denensin.</summary>
    private const int MaxNumberAttempts = 3;

    public async Task<ReceiptDto> Handle(CreateReceiptCommand request, CancellationToken ct)
    {
        // ── Müşteri ve randevu bu işletmeye ait mi ────────────────────────────
        // Önceden kimlikler olduğu gibi kabul ediliyordu: bir işletme fişini
        // başka bir işletmenin müşterisine bağlayabiliyor, yanıtta da o
        // müşterinin adını okuyabiliyordu.
        string? customerName = null;
        if (request.CustomerId.HasValue)
        {
            customerName = await _context.Customers
                .Where(c => c.Id == request.CustomerId.Value && c.TenantId == request.TenantId)
                .Select(c => c.FullName)
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("Müşteri", request.CustomerId.Value);
        }

        if (request.AppointmentId.HasValue)
        {
            var appointment = await _context.Appointments
                .Where(a => a.Id == request.AppointmentId.Value && a.TenantId == request.TenantId)
                .Select(a => new { a.CustomerId })
                .FirstOrDefaultAsync(ct)
                ?? throw new NotFoundException("Randevu", request.AppointmentId.Value);

            if (request.CustomerId.HasValue && appointment.CustomerId != request.CustomerId.Value)
                throw new BadRequestException("Randevu bu müşteriye ait değil.");
        }

        // Ad baştan/sondan kırpılıyor, tutar kuruşa yuvarlanıyor: sütun
        // numeric(18,2) ve toplam kalemlerin toplamıyla birebir aynı olmalı.
        var lines = request.Items
            .Select(i => new
            {
                Name      = i.ServiceName.Trim(),
                i.Quantity,
                UnitPrice = Math.Round(i.UnitPrice, 2, MidpointRounding.AwayFromZero),
            })
            .ToList();

        // ── Fiş numarası ──────────────────────────────────────────────────────
        // (TenantId, ReceiptNumber) benzersiz. İki fiş aynı anda kesilirse ikisi
        // de aynı "son numarayı" okuyup aynı numarayı üretiyor ve ikincisi
        // veritabanında patlıyordu. Çakışmada numara yeniden hesaplanıp
        // tekrar deneniyor.
        for (var attempt = 1; ; attempt++)
        {
            var receiptNumber = await NextReceiptNumberAsync(request.TenantId, ct);

            var items = lines.Select(l => new ReceiptItemEntity
            {
                ServiceName = l.Name,
                Quantity    = l.Quantity,
                UnitPrice   = l.UnitPrice,
            }).ToList();

            var receipt = new ReceiptEntity
            {
                TenantId      = request.TenantId,
                CustomerId    = request.CustomerId,
                AppointmentId = request.AppointmentId,
                ReceiptNumber = receiptNumber,
                TotalAmount   = items.Sum(i => i.Quantity * i.UnitPrice),
                Note          = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim(),
                Items         = items,
            };

            _context.Receipts.Add(receipt);
            try
            {
                await _context.SaveChangesAsync(ct);
                return ToDto(receipt, customerName);
            }
            catch (DbUpdateException) when (attempt < MaxNumberAttempts)
            {
                // Eklenmiş ama kaydedilememiş kayıtlar takipten çıkarılıyor ki
                // bir sonraki deneme aynı numarayı tekrar göndermesin.
                _context.ReceiptItems.RemoveRange(items);
                _context.Receipts.Remove(receipt);
            }
            catch (DbUpdateException)
            {
                throw new ConflictException(
                    "Fiş numarası oluşturulamadı, lütfen tekrar deneyin.");
            }
        }
    }

    /// <summary>
    /// Yılın sıradaki numarası: 2026-0001, 2026-0002 …
    ///
    /// Silinmiş fişler de sayılıyor (IgnoreQueryFilters): benzersizlik dizini
    /// onları kapsıyor, sayılmazlarsa silinen bir fişin numarası yeniden
    /// üretilip sonsuza kadar çakışırdı. Sıralama önce uzunluğa göre, çünkü
    /// metin olarak "2026-10000", "2026-9999"dan küçük sıralanır.
    /// </summary>
    private async Task<string> NextReceiptNumberAsync(Guid tenantId, CancellationToken ct)
    {
        var year   = DateTime.UtcNow.Year;
        var prefix = year + "-";

        var lastNumber = await _context.Receipts
            .IgnoreQueryFilters()
            .Where(r => r.TenantId == tenantId && r.ReceiptNumber.StartsWith(prefix))
            .OrderByDescending(r => r.ReceiptNumber.Length)
            .ThenByDescending(r => r.ReceiptNumber)
            .Select(r => r.ReceiptNumber)
            .FirstOrDefaultAsync(ct);

        var seq = 1;
        if (lastNumber != null && int.TryParse(lastNumber[prefix.Length..], out var last))
            seq = last + 1;

        return $"{prefix}{seq:D4}";
    }

    internal static ReceiptDto ToDto(ReceiptEntity r, string? customerName) => new()
    {
        Id            = r.Id,
        ReceiptNumber = r.ReceiptNumber,
        CustomerName  = customerName,
        CustomerId    = r.CustomerId,
        AppointmentId = r.AppointmentId,
        TotalAmount   = r.TotalAmount,
        Currency      = r.Currency,
        Note          = r.Note,
        IsVoid        = r.IsVoid,
        CreatedAt     = r.CreatedAt,
        Items         = r.Items.Select(i => new ReceiptItemDto
        {
            Id          = i.Id,
            ServiceName = i.ServiceName,
            Quantity    = i.Quantity,
            UnitPrice   = i.UnitPrice,
        }).ToList(),
    };
}
