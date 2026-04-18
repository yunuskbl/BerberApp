using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class WorkingHourConfiguration : IEntityTypeConfiguration<WorkingHour>
{
    public void Configure(EntityTypeBuilder<WorkingHour> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DayOfWeek).IsRequired();
        builder.Property(x => x.StartTime).IsRequired();
        builder.Property(x => x.EndTime).IsRequired();
        builder.HasIndex(x => new { x.StaffId, x.DayOfWeek });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Staff)
               .WithMany(x => x.WorkingHours)
               .HasForeignKey(x => x.StaffId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
