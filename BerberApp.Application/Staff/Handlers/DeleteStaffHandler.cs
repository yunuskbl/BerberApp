using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using MediatR;

namespace BerberApp.Application.Staff.Handlers;

public class DeleteStaffHandler : IRequestHandler<DeleteStaffCommand, bool>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;

    public DeleteStaffHandler(IGenericRepository<StaffEntity> staffRepo)
    {
        _staffRepo = staffRepo;
    }

    public async Task<bool> Handle(DeleteStaffCommand request, CancellationToken ct)
    {
        var staff = await _staffRepo.GetAsync(
            x => x.Id == request.Id && x.TenantId == request.TenantId, ct);

        if (staff is null)
            throw new NotFoundException("Personel", request.Id);

        await _staffRepo.DeleteAsync(staff, ct);
        return true;
    }
}