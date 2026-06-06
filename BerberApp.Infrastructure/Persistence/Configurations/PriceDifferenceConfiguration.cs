using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class PriceDifferenceConfiguration : IEntityTypeConfiguration<PriceDifference>
{
    public void Configure(EntityTypeBuilder<PriceDifference> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OriginalPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualPrice).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Difference).HasColumnType("decimal(18,2)");
        builder.HasIndex(x => x.TenantId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Tenant)
               .WithMany()
               .HasForeignKey(x => x.TenantId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Appointment)
               .WithMany()
               .HasForeignKey(x => x.AppointmentId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}
