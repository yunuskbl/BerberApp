using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using BerberApp.Domain.Entities;
using MediatR;

namespace BerberApp.Application.Staff.Handlers;

public class BroadcastStaffMessageHandler : IRequestHandler<BroadcastStaffMessageCommand, BroadcastStaffMessageResult>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;
    private readonly IWhatsAppService _whatsAppService;

    public BroadcastStaffMessageHandler(
        IGenericRepository<StaffEntity> staffRepo,
        IWhatsAppService whatsAppService)
    {
        _staffRepo = staffRepo;
        _whatsAppService = whatsAppService;
    }

    public async Task<BroadcastStaffMessageResult> Handle(BroadcastStaffMessageCommand request, CancellationToken ct)
    {
        var staffList = await _staffRepo.GetAllAsync(x => x.TenantId == request.TenantId && !x.IsDeleted, ct);

        int sent = 0, failed = 0;

        foreach (var staff in staffList)
        {
            if (string.IsNullOrWhiteSpace(staff.Phone)) { failed++; continue; }
            try
            {
                await _whatsAppService.SendCustomMessageAsync(staff.Phone, request.Message);
                sent++;
            }
            catch
            {
                failed++;
            }
        }

        return new BroadcastStaffMessageResult
        {
            TotalStaff = staffList.Count,
            Sent = sent,
            Failed = failed,
        };
    }
}
