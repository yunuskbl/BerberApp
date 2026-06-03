namespace BerberApp.Application.Common.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(
        string eventType,
        string severity,
        string ipAddress,
        string path,
        string method,
        string? userId = null,
        string? tenantId = null,
        string? userAgent = null,
        string? description = null);
}
