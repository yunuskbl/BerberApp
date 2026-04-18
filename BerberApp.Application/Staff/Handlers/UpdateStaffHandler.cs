using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using BerberApp.Application.Staff.DTOs;
using MediatR;

namespace BerberApp.Application.Staff.Handlers;

public class UpdateStaffHandler : IRequestHandler<UpdateStaffCommand, StaffDto>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public UpdateStaffHandler(IGenericRepository<StaffEntity> staffRepo)
    {
        _staffRepo = staffRepo;
    }

    public async Task<StaffDto> Handle(UpdateStaffCommand request, CancellationToken ct)
    {
        var staff = await _staffRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (staff is null)
            throw new NotFoundException("Personel", request.Id);

        staff.FullName = request.FullName;
        staff.Phone = request.Phone;
        staff.AvatarUrl = request.AvatarUrl;
        staff.Bio = request.Bio;
        staff.IsActive = request.IsActive;

        await _staffRepo.UpdateAsync(staff, ct);

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