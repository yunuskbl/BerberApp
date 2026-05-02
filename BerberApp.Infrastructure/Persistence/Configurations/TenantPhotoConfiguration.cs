using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class TenantPhotoConfiguration : IEntityTypeConfiguration<TenantPhoto>
{
    public void Configure(EntityTypeBuilder<TenantPhoto> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Url).IsRequired().HasMaxLength(500);
        builder.HasQueryFilter(x => !x.IsDeleted);
        builder.HasOne(x => x.Tenant)
               .WithMany(x => x.Photos)
               .HasForeignKey(x => x.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
