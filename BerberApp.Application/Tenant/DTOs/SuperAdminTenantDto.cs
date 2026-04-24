namespace BerberApp.Application.Tenant.DTOs;

/// <summary>
/// SuperAdmin paneli için tenant bilgileri (istatistikler ile)
/// </summary>
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

    // İstatistikler
    public int StaffCount { get; set; }
    public int CustomerCount { get; set; }
    public int TotalAppointments { get; set; }
    public int PendingAppointments { get; set; }
    public int CompletedAppointments { get; set; }
    public string? Plan { get; set; }
}
