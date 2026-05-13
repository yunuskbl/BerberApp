using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

internal class StaffServiceConfiguration : IEntityTypeConfiguration<StaffService>
{
    public void Configure(EntityTypeBuilder<StaffService> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.CustomPrice)
               .HasColumnType("decimal(18,2)");

        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Staff)
               .WithMany(x => x.StaffServices)
               .HasForeignKey(x => x.StaffId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Service)
               .WithMany(x => x.StaffServices)
               .HasForeignKey(x => x.ServiceId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable("StaffServices");
    }
}
