using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Common.Broadcast;

/// <summary>Bir toplu mesaj işinin ilerlemesini ve alıcı sonuçlarını sorgular.</summary>
public class GetBroadcastStatusQuery : IRequest<BroadcastStatusDto>
{
    public Guid TenantId { get; set; }
    public Guid BroadcastJobId { get; set; }
}

public class BroadcastStatusDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsFinished { get; set; }
    public string? Error { get; set; }

    public int Total { get; set; }
    public int Sent { get; set; }
    public int Failed { get; set; }
    public int Skipped { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    /// <summary>Yalnızca sorunlu alıcılar — sebebiyle birlikte.</summary>
    public List<BroadcastProblemDto> Problems { get; set; } = [];
}

public class BroadcastProblemDto
{
    public string? Name { get; set; }
    public string? Phone { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Error { get; set; }
}

public class GetBroadcastStatusHandler : IRequestHandler<GetBroadcastStatusQuery, BroadcastStatusDto>
{
    private readonly IAppDbContext _context;

    public GetBroadcastStatusHandler(IAppDbContext context) => _context = context;

    public async Task<BroadcastStatusDto> Handle(GetBroadcastStatusQuery request, CancellationToken ct)
    {
        var job = await _context.BroadcastJobs
            .AsNoTracking()
            .Include(j => j.Recipients)
            .FirstOrDefaultAsync(j => j.Id == request.BroadcastJobId && j.TenantId == request.TenantId, ct);

        if (job is null)
            throw new NotFoundException("Toplu mesaj", request.BroadcastJobId);

        return new BroadcastStatusDto
        {
            Id          = job.Id,
            Status      = job.Status.ToString(),
            IsFinished  = job.Status is BroadcastStatus.Completed or BroadcastStatus.Failed,
            Error       = job.Error,
            Total       = job.Total,
            Sent        = job.Sent,
            Failed      = job.Failed,
            Skipped     = job.Skipped,
            CreatedAt   = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            Problems    = job.Recipients
                .Where(r => r.Status is BroadcastRecipientStatus.Failed or BroadcastRecipientStatus.Skipped)
                .OrderBy(r => r.Name)
                .Select(r => new BroadcastProblemDto
                {
                    Name   = r.Name,
                    Phone  = r.Phone,
                    Status = r.Status.ToString(),
                    Error  = r.Error,
                })
                .ToList(),
        };
    }
}
