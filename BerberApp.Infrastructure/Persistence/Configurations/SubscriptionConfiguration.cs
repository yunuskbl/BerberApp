using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

/// <summary>
/// Subscription table configuration
/// </summary>
public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.ToTable("Subscriptions");

        builder.HasKey(x => x.Id);

        // Plan as string (enum)
        builder.Property(x => x.Plan)
            .HasConversion<string>();

        // Status as string (enum)
        builder.Property(x => x.Status)
            .HasConversion<string>();

        // Currency
        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .HasDefaultValue("TRY");

        // Price
        builder.Property(x => x.Price)
            .HasPrecision(10, 2);

        // Tenant relationship
        builder.HasOne(x => x.Tenant)
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(x => x.TenantId);
        builder.HasIndex(x => new { x.TenantId, x.Status });
    }
}