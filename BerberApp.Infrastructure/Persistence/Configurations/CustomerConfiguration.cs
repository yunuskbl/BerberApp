using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.FullName).IsRequired().HasMaxLength(100);
        builder.Property(x => x.Phone).IsRequired().HasMaxLength(20);
        builder.Property(x => x.Email).HasMaxLength(150);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.TenantId, x.Phone });
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Tenant)
               .WithMany(x => x.Customers)
               .HasForeignKey(x => x.TenantId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
