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

    public BroadcastStaffMessageHandler(
        IGenericRepository<StaffEntity> staffRepo,
        IAppDbContext context,
        IWhatsAppService whatsAppService)
    {
        _staffRepo = staffRepo;
        _context = context;
        _whatsAppService = whatsAppService;
    }

    public async Task<BroadcastStaffMessageResult> Handle(BroadcastStaffMessageCommand request, CancellationToken ct)
    {
        var staffList = await _staffRepo.GetAllAsync(x => x.TenantId == request.TenantId && !x.IsDeleted, ct);

        // İşletme kendi WhatsApp'ını bağladıysa toplu mesaj kendi numarasından
        // çıkar; bağlamadıysa kök servis (merkezi hat → Meta) kullanılır.
        var tenant = await _context.Tenants.AsNoTracking()
            .Where(t => t.Id == request.TenantId)
            .Select(t => new { t.WppConnectSession, t.WppConnectToken })
            .FirstOrDefaultAsync(ct);

        var wa = (!string.IsNullOrWhiteSpace(tenant?.WppConnectSession) && !string.IsNullOrWhiteSpace(tenant?.WppConnectToken))
            ? _whatsAppService.ForTenant(tenant!.WppConnectSession, tenant.WppConnectToken)
            : _whatsAppService;

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
