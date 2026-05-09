using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Rating).IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(1000);
        builder.HasIndex(x => x.TenantId);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Tenant)
               .WithMany()
               .HasForeignKey(x => x.TenantId)
               .OnDelete(DeleteBehavior.Cascade);

        // Appointment navigation — optional to avoid cross-filter issues
        builder.HasOne(x => x.Appointment)
               .WithMany()
               .HasForeignKey(x => x.AppointmentId)
               .OnDelete(DeleteBehavior.Restrict)
               .IsRequired(false);
    }
}
