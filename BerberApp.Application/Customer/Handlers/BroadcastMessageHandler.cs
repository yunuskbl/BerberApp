using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.Commands;
using BerberApp.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Customer.Handlers;

public class BroadcastMessageHandler : IRequestHandler<BroadcastMessageCommand, BroadcastMessageResult>
{
    private readonly IAppDbContext _context;
    private readonly IWhatsAppService _whatsAppService;

    public BroadcastMessageHandler(IAppDbContext context, IWhatsAppService whatsAppService)
    {
        _context = context;
        _whatsAppService = whatsAppService;
    }

    public async Task<BroadcastMessageResult> Handle(BroadcastMessageCommand request, CancellationToken ct)
    {
        var allCustomers = await _context.Customers
            .Where(c => c.TenantId == request.TenantId)
            .Select(c => new { c.Id, c.Phone })
            .ToListAsync(ct);

        var totalCustomers = allCustomers.Count;

        var targetIds = request.Filter switch
        {
            BroadcastFilter.NotVisitedSince => await GetNotVisitedSinceAsync(request, ct),
            BroadcastFilter.NeverVisited    => await GetNeverVisitedAsync(request, ct),
            BroadcastFilter.FrequentCustomers => await GetFrequentCustomersAsync(request, ct),
            BroadcastFilter.RecentVisitors  => await GetRecentVisitorsAsync(request, ct),
            _                               => allCustomers.Select(c => c.Id).ToHashSet()
        };

        var targets = allCustomers.Where(c => targetIds.Contains(c.Id)).ToList();

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

        foreach (var customer in targets)
        {
            if (string.IsNullOrWhiteSpace(customer.Phone)) { failed++; continue; }
            try
            {
                await wa.SendCustomMessageAsync(customer.Phone, request.Message, request.ImageUrl);
                sent++;
            }
            catch
            {
                failed++;
            }
        }

        return new BroadcastMessageResult
        {
            TotalCustomers = totalCustomers,
            FilteredCount  = targets.Count,
            Sent           = sent,
            Failed         = failed,
        };
    }

    private async Task<HashSet<Guid>> GetNotVisitedSinceAsync(BroadcastMessageCommand req, CancellationToken ct)
    {
        var days = req.FilterDays ?? 30;
        var cutoff = DateTime.UtcNow.AddDays(-days);

        // Son X günde tamamlanmış randevusu olan müşteriler
        var recentVisitorIds = await _context.Appointments
            .Where(a => a.TenantId == req.TenantId
                     && a.Status == AppointmentStatus.Completed
                     && a.StartTime >= cutoff)
            .Select(a => a.CustomerId)
            .Distinct()
            .ToListAsync(ct);

        // Hiç randevusu olmayanlar dahil, son X günde gelmeyenlerin tamamı
        var allIds = await _context.Customers
            .Where(c => c.TenantId == req.TenantId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        return allIds.Except(recentVisitorIds).ToHashSet();
    }

    private async Task<HashSet<Guid>> GetNeverVisitedAsync(BroadcastMessageCommand req, CancellationToken ct)
    {
        var visitedIds = await _context.Appointments
            .Where(a => a.TenantId == req.TenantId && a.Status == AppointmentStatus.Completed)
            .Select(a => a.CustomerId)
            .Distinct()
            .ToListAsync(ct);

        var allIds = await _context.Customers
            .Where(c => c.TenantId == req.TenantId)
            .Select(c => c.Id)
            .ToListAsync(ct);

        return allIds.Except(visitedIds).ToHashSet();
    }

    private async Task<HashSet<Guid>> GetFrequentCustomersAsync(BroadcastMessageCommand req, CancellationToken ct)
    {
        var min = req.MinAppointments ?? 3;

        return (await _context.Appointments
            .Where(a => a.TenantId == req.TenantId && a.Status == AppointmentStatus.Completed)
            .GroupBy(a => a.CustomerId)
            .Where(g => g.Count() >= min)
            .Select(g => g.Key)
            .ToListAsync(ct))
            .ToHashSet();
    }

    private async Task<HashSet<Guid>> GetRecentVisitorsAsync(BroadcastMessageCommand req, CancellationToken ct)
    {
        var days = req.FilterDays ?? 30;
        var cutoff = DateTime.UtcNow.AddDays(-days);

        return (await _context.Appointments
            .Where(a => a.TenantId == req.TenantId
                     && a.Status == AppointmentStatus.Completed
                     && a.StartTime >= cutoff)
            .Select(a => a.CustomerId)
            .Distinct()
            .ToListAsync(ct))
            .ToHashSet();
    }
}
