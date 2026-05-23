using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class Staff : BaseEntity
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? AvatarUrl { get; set; }
    public string? Bio { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public ICollection<StaffService> StaffServices { get; set; } = new List<StaffService>();
    public ICollection<WorkingHour> WorkingHours { get; set; } = new List<WorkingHour>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<StaffDayOff> DaysOff { get; set; } = new List<StaffDayOff>();
}
