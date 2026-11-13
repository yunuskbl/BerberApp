using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

// Seans paketleri. Sınırlar API doğrulamasıyla aynı (PackagesController).

public class PackageTemplateConfiguration : IEntityTypeConfiguration<PackageTemplate>
{
    public void Configure(EntityTypeBuilder<PackageTemplate> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Price).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("TRY");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Tenant).WithMany()
               .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Service).WithMany()
               .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.TenantId);
    }
}

public class CustomerPackageConfiguration : IEntityTypeConfiguration<CustomerPackage>
{
    public void Configure(EntityTypeBuilder<CustomerPackage> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(150);
        builder.Property(x => x.Price).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("TRY");
        builder.Property(x => x.PublicToken).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.CancelReason).HasMaxLength(300);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Tenant).WithMany()
               .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Customer).WithMany()
               .HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.PackageTemplate).WithMany()
               .HasForeignKey(x => x.PackageTemplateId).OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(x => x.Service).WithMany()
               .HasForeignKey(x => x.ServiceId).OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(x => x.Sessions).WithOne(x => x.CustomerPackage)
               .HasForeignKey(x => x.CustomerPackageId).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Payments).WithOne(x => x.CustomerPackage)
               .HasForeignKey(x => x.CustomerPackageId).OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.PublicToken).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.Status });
        builder.HasIndex(x => new { x.TenantId, x.CustomerId });
    }
}

public class PackageSessionConfiguration : IEntityTypeConfiguration<PackageSession>
{
    public void Configure(EntityTypeBuilder<PackageSession> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Note).HasMaxLength(500);
        builder.Property(x => x.ConfirmCode).HasMaxLength(64);
        builder.Property(x => x.VoidReason).HasMaxLength(300);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Staff).WithMany()
               .HasForeignKey(x => x.StaffId).OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.CustomerPackageId);
        builder.HasIndex(x => x.ConfirmCode).IsUnique().HasFilter("\"ConfirmCode\" IS NOT NULL");
    }
}

public class PackagePaymentConfiguration : IEntityTypeConfiguration<PackagePayment>
{
    public void Configure(EntityTypeBuilder<PackagePayment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).HasColumnType("numeric(18,2)");
        builder.Property(x => x.Currency).IsRequired().HasMaxLength(3).HasDefaultValue("TRY");
        builder.Property(x => x.Note).HasMaxLength(300);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.CustomerPackageId);
    }
}
