using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Endpoint).IsRequired().HasMaxLength(1024);
        builder.Property(x => x.P256dh).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Auth).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Platform).HasMaxLength(20);
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Tenant).WithMany()
               .HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.User).WithMany()
               .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);

        // Aynı cihaz aboneliği bir kez; kullanıcı değişirse (çıkış/giriş) sahibi güncellenir.
        builder.HasIndex(x => x.Endpoint).IsUnique();
        builder.HasIndex(x => x.TenantId);
    }
}
