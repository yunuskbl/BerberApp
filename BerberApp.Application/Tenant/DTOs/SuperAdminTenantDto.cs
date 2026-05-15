namespace BerberApp.Application.Tenant.DTOs;

public class SuperAdminTenantDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public string? LogoUrl { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }

    // Admin bilgisi
    public string? AdminEmail { get; set; }
    public string? AdminName { get; set; }

    // İstatistikler
    public int StaffCount { get; set; }
    public int CustomerCount { get; set; }
    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int CompletedAppointments { get; set; }

    // Abonelik bilgisi
    public string? Plan { get; set; }
    public string? SubscriptionStatus { get; set; }
    public DateTime? SubscriptionExpiresAt { get; set; }
    public bool IsOnTrial { get; set; }
    public int? DaysLeft { get; set; }
}
