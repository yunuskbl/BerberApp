namespace BerberApp.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string EventType { get; set; } = string.Empty;   // LOGIN_SUCCESS, LOGIN_FAILED, UNAUTHORIZED, FORBIDDEN, RATE_LIMITED, SUPERADMIN_ACTION
    public string Severity { get; set; } = "Info";          // Info | Warning | Critical
    public string? UserId { get; set; }
    public string? TenantId { get; set; }
    public string IpAddress { get; set; } = string.Empty;
    public string? UserAgent { get; set; }
    public string Path { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
