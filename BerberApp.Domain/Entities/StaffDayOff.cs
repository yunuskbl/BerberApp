using BerberApp.Domain.Common;

namespace BerberApp.Domain.Entities;

public class StaffDayOff : BaseEntity
{
    public Guid StaffId { get; set; }
    public DateOnly Date { get; set; }
    public string? Reason { get; set; }

    // Navigation
    public Staff Staff { get; set; } = null!;
}
