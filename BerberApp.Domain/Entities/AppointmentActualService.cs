using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class AppointmentActualService : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public Guid ServiceId { get; set; }
    public decimal Price { get; set; }

    public Appointment Appointment { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
