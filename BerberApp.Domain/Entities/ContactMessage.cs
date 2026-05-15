using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class ContactMessage : BaseEntity
{
    public Guid TenantId { get; set; }
    public string TenantName { get; set; } = string.Empty;
    public string SenderEmail { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Status { get; set; } = "New";
    public string? Reply { get; set; }
    public DateTime? RepliedAt { get; set; }
    public bool IsReplySeen { get; set; } = false;
}
