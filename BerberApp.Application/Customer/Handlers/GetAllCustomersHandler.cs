using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.DTOs;
using BerberApp.Application.Customer.Queries;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Customer.Handlers;

public class GetAllCustomersHandler : IRequestHandler<GetAllCustomersQuery, List<CustomerDto>>
{
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;

    public GetAllCustomersHandler(
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<AppointmentEntity> appointmentRepo)
    {
        _customerRepo = customerRepo;
        _appointmentRepo = appointmentRepo;
    }

    public async Task<List<CustomerDto>> Handle(GetAllCustomersQuery request, CancellationToken ct)
    {
        var list = await _customerRepo.GetAllAsync(x => x.TenantId == request.TenantId, ct);

        // Ziyaret sayısı randevulardan sayılıyor. Customer.TotalVisits sütunu
        // duruyor ama onu artıran tek bir satır kod yok — yalnızca seed verisi
        // dolduruyordu, bu yüzden listede her gerçek müşteri "0 ziyaret"
        // görünüyordu. Sayıyı denormalize edip her yazma yolunda güncel tutmak
        // yerine okurken saymak, yanlış veriye düşme ihtimalini tümden kaldırıyor.
        var completed = await _appointmentRepo.GetAllAsync(
            x => x.TenantId == request.TenantId &&
                 x.Status == AppointmentStatus.Completed, ct);

        var visits = completed
            .Where(x => x.CustomerId != Guid.Empty)
            .GroupBy(x => x.CustomerId)
            .ToDictionary(g => g.Key, g => new
            {
                Count = g.Count(),
                Last  = g.Max(x => x.StartTime),
            });

        return list.Select(c =>
        {
            visits.TryGetValue(c.Id, out var v);
            return new CustomerDto
            {
                Id          = c.Id,
                FullName    = c.FullName,
                Phone       = c.Phone,
                Email       = c.Email,
                Notes       = c.Notes,
                TotalVisits = v?.Count ?? 0,
                LastVisitAt = v?.Last,
            };
        }).ToList();
    }
}
