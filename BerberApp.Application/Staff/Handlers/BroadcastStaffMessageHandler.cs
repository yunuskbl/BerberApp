using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Staff.Commands;
using BerberApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Staff.Handlers;

public class BroadcastStaffMessageHandler : IRequestHandler<BroadcastStaffMessageCommand, BroadcastStaffMessageResult>
{
    private readonly IGenericRepository<StaffEntity> _staffRepo;
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;
    private readonly IWppConnectManagementService _wppManagement;

    public BroadcastStaffMessageHandler(
        IGenericRepository<StaffEntity> staffRepo,
        IAppDbContext context,
        IWhatsAppService whatsAppService,
        IWppConnectManagementService wppManagement)
    {
        _staffRepo = staffRepo;
        _context = context;
        _whatsAppService = whatsAppService;
        _wppManagement = wppManagement;
    }

    public async Task<BroadcastStaffMessageResult> Handle(BroadcastStaffMessageCommand request, CancellationToken ct)
    {
        var staffList = await _staffRepo.GetAllAsync(x => x.TenantId == request.TenantId && !x.IsDeleted, ct);

        var wa = await WhatsAppLineResolver.RequireTenantLineAsync(
            _context, _whatsAppService, _wppManagement, request.TenantId, ct);

        int sent = 0, failed = 0;

        foreach (var staff in staffList)
        {
            if (string.IsNullOrWhiteSpace(staff.Phone)) { failed++; continue; }
            try
            {
                await wa.SendCustomMessageAsync(staff.Phone, request.Message);
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
