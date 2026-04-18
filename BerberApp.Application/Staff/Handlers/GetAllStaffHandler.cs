using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.DTOs;
using BerberApp.Application.Staff.Queries;
using MediatR;

namespace BerberApp.Application.Staff.Handlers;

public class GetAllStaffHandler : IRequestHandler<GetAllStaffQuery, List<StaffDto>>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public GetAllStaffHandler(IGenericRepository<StaffEntity> staffRepo)
    {
        _staffRepo = staffRepo;
    }

    public async Task<List<StaffDto>> Handle(GetAllStaffQuery request, CancellationToken ct)
    {
        var list = await _staffRepo.GetAllAsync(x => x.TenantId == request.TenantId, ct);

        return list.Select(s => new StaffDto
        {
            Id = s.Id,
            FullName = s.FullName,
            Phone = s.Phone,
            AvatarUrl = s.AvatarUrl,
            Bio = s.Bio,
            IsActive = s.IsActive
        }).ToList();
    }
}