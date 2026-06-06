using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Common;
using BerberApp.Domain.Enums;

namespace BerberApp.Domain.Entities;

public class Appointment : BaseEntity
{
    public Guid TenantId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public bool ReminderSent24h { get; set; } = false;
    public bool ReminderSent1h  { get; set; } = false;

    // Tamamlama alanları
    public decimal? ActualTotalPrice { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? CompletionNotes { get; set; }

    // Navigation
    public Tenant Tenant { get; set; } = null!;
    public Customer Customer { get; set; } = null!;
    public Staff Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
    public ICollection<AppointmentActualService> ActualServices { get; set; } = new List<AppointmentActualService>();
}
