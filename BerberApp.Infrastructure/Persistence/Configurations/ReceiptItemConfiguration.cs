using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class ReceiptItemConfiguration : IEntityTypeConfiguration<ReceiptItem>
{
    public void Configure(EntityTypeBuilder<ReceiptItem> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ServiceName).IsRequired().HasMaxLength(200);
        builder.Property(x => x.UnitPrice).HasColumnType("numeric(18,2)");
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasIndex(x => x.ReceiptId);
    }
}
