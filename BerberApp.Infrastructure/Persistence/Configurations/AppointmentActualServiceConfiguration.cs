using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class AppointmentActualServiceConfiguration : IEntityTypeConfiguration<AppointmentActualService>
{
    public void Configure(EntityTypeBuilder<AppointmentActualService> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Appointment)
               .WithMany(x => x.ActualServices)
               .HasForeignKey(x => x.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Service)
               .WithMany()
               .HasForeignKey(x => x.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
