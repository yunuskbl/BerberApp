using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using BerberApp.Application.Staff.DTOs;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Staff.Handlers;

public class CreateStaffHandler : IRequestHandler<CreateStaffCommand, StaffDto>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;
    private readonly IGenericRepository<TenantEntity> _tenantRepo;
    private readonly IGenericRepository<SubscriptionEntity> _subscriptionRepo;

    public CreateStaffHandler(
        IGenericRepository<StaffEntity> staffRepo,
        IGenericRepository<TenantEntity> tenantRepo,
        IGenericRepository<SubscriptionEntity> subscriptionRepo)
    {
        _staffRepo = staffRepo;
        _tenantRepo = tenantRepo;
        _subscriptionRepo = subscriptionRepo;
    }

    public async Task<StaffDto> Handle(CreateStaffCommand request, CancellationToken ct)
    {
        var tenantExists = await _tenantRepo.AnyAsync(x => x.Id == request.TenantId, ct);
        if (!tenantExists)
            throw new NotFoundException("Tenant", request.TenantId);

        // Plan limit kontrolü
        var subscription = await _subscriptionRepo.GetAsync(
            x => x.TenantId == request.TenantId && x.Status == SubscriptionStatus.Active, ct);
        var plan = subscription?.Plan ?? PlanType.Baslangic;

        int staffLimit = plan switch
        {
            PlanType.Baslangic   => 1,
            PlanType.Profesyonel => 5,
            _                    => int.MaxValue
        };

        if (staffLimit < int.MaxValue)
        {
            int currentCount = await _staffRepo.CountAsync(
                x => x.TenantId == request.TenantId, ct);

            if (currentCount >= staffLimit)
                throw new BadRequestException(
                    $"Mevcut planınız en fazla {staffLimit} personel desteklemektedir. " +
                    "Daha fazla personel eklemek için planınızı yükseltin.");
        }

        var staff = new StaffEntity
        {
            TenantId = request.TenantId,
            FullName = request.FullName,
            Phone = request.Phone,
            AvatarUrl = request.AvatarUrl,
            Bio = request.Bio,
            IsActive = true
        };

        await _staffRepo.AddAsync(staff, ct);

        return ToDto(staff);
    }

    private static StaffDto ToDto(StaffEntity s) => new()
    {
        Id = s.Id,
        FullName = s.FullName,
        Phone = s.Phone,
        AvatarUrl = s.AvatarUrl,
        Bio = s.Bio,
        IsActive = s.IsActive
    };
}