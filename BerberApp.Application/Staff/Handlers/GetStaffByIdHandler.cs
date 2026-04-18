using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.DTOs;
using BerberApp.Application.Staff.Queries;
using MediatR;

namespace BerberApp.Application.Staff.Handlers;

public class GetStaffByIdHandler : IRequestHandler<GetStaffByIdQuery, StaffDto>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public GetStaffByIdHandler(IGenericRepository<StaffEntity> staffRepo)
    {
        _staffRepo = staffRepo;
    }

    public async Task<StaffDto> Handle(GetStaffByIdQuery request, CancellationToken ct)
    {
        var staff = await _staffRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (staff is null)
            throw new NotFoundException("Personel", request.Id);

        return new StaffDto
        {
            Id = staff.Id,
            FullName = staff.FullName,
            Phone = staff.Phone,
            AvatarUrl = staff.AvatarUrl,
            Bio = staff.Bio,
            IsActive = staff.IsActive
        };
    }
}