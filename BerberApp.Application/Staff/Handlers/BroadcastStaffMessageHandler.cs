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
    private readonly IBroadcastQueue _queue;

    public BroadcastStaffMessageHandler(
        IGenericRepository<StaffEntity> staffRepo,
        IAppDbContext context,
        IBroadcastQueue queue)
    {
        _staffRepo = staffRepo;
        _context   = context;
        _queue     = queue;
    }

    public async Task<BroadcastStaffMessageResult> Handle(BroadcastStaffMessageCommand request, CancellationToken ct)
    {
        var staffList = await _staffRepo.GetAllAsync(x => x.TenantId == request.TenantId && !x.IsDeleted, ct);

        // Gönderim burada yapılmıyor — iş kaydı yazılıp kuyruğa veriliyor,
        // istek anında dönüyor. Alıcı bazında sonuç BroadcastRecipient'ta tutulur.
        var job = new BroadcastJob
        {
            TenantId = request.TenantId,
            Kind     = BroadcastKind.Staff,
            Message  = request.Message,
            Total    = staffList.Count,
        };

        foreach (var staff in staffList)
        {
            job.Recipients.Add(new BroadcastRecipient
            {
                Name  = staff.FullName,
                Phone = staff.Phone,
            });
        }

        _context.BroadcastJobs.Add(job);
        await _context.SaveChangesAsync(ct);

        _queue.Enqueue(job.Id);

        return new BroadcastStaffMessageResult
        {
            BroadcastJobId = job.Id,
            TotalStaff     = staffList.Count,
        };
    }
}
