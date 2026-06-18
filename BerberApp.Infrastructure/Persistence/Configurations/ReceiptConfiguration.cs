using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class ReceiptConfiguration : IEntityTypeConfiguration<Receipt>
{
    public void Configure(EntityTypeBuilder<Receipt> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ReceiptNumber).IsRequired().HasMaxLength(20);
        builder.Property(x => x.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Currency).HasMaxLength(3).HasDefaultValue("TRY");
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Tenant)
               .WithMany()
               .HasForeignKey(x => x.TenantId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Customer)
               .WithMany()
               .HasForeignKey(x => x.CustomerId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.Appointment)
               .WithMany()
               .HasForeignKey(x => x.AppointmentId)
               .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Items)
               .WithOne(x => x.Receipt)
               .HasForeignKey(x => x.ReceiptId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.TenantId, x.ReceiptNumber }).IsUnique();
        builder.HasIndex(x => x.AppointmentId);
    }
}
