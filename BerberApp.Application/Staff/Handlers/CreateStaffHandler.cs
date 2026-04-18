using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using BerberApp.Application.Staff.DTOs;
using MediatR;

namespace BerberApp.Application.Staff.Handlers;

public class CreateStaffHandler : IRequestHandler<CreateStaffCommand, StaffDto>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;
    private readonly IGenericRepository<TenantEntity> _tenantRepo;

    public CreateStaffHandler(
        IGenericRepository<StaffEntity> staffRepo,
        IGenericRepository<TenantEntity> tenantRepo)
    {
        _staffRepo = staffRepo;
        _tenantRepo = tenantRepo;
    }

    public async Task<StaffDto> Handle(CreateStaffCommand request, CancellationToken ct)
    {
        var tenantExists = await _tenantRepo.AnyAsync(x => x.Id == request.TenantId, ct);
        if (!tenantExists)
            throw new NotFoundException("Tenant", request.TenantId);

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