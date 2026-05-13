using BerberApp.Domain.Commons;

namespace BerberApp.Domain.Entities;

public class StaffService : BaseEntity
{
    public Guid StaffId { get; set; }
    public Guid ServiceId { get; set; }

    public decimal? CustomPrice { get; set; }
    public int? CustomDurationMinutes { get; set; }

    public Staff Staff { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
