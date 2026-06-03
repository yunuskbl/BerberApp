using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Infrastructure.Persistence;

namespace BerberApp.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(
        string eventType,
        string severity,
        string ipAddress,
        string path,
        string method,
        string? userId = null,
        string? tenantId = null,
        string? userAgent = null,
        string? description = null)
    {
        var log = new AuditLog
        {
            EventType   = eventType,
            Severity    = severity,
            IpAddress   = ipAddress,
            Path        = path,
            Method      = method,
            UserId      = userId,
            TenantId    = tenantId,
            UserAgent   = userAgent?.Length > 200 ? userAgent[..200] + "…" : userAgent,
            Description = description,
            CreatedAt   = DateTime.UtcNow,
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
