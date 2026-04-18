using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Status).IsRequired();
        builder.HasIndex(x => new { x.TenantId, x.StartTime });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Customer)
               .WithMany(x => x.Appointments)
               .HasForeignKey(x => x.CustomerId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Staff)
               .WithMany(x => x.Appointments)
               .HasForeignKey(x => x.StaffId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Service)
               .WithMany()
               .HasForeignKey(x => x.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
