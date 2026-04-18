using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Common;
using BerberApp.Domain.Enums;

namespace BerberApp.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public NotificationChannel Channel { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsSent { get; set; } = false;
    public DateTime? SentAt { get; set; }

    // Navigation
    public Appointment Appointment { get; set; } = null!;
}
