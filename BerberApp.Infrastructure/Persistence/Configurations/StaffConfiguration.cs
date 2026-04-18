using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations
{
    internal class StaffConfiguration:IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> builder)
        {
            builder.HasKey(x => x.Id);
            builder.Property(x => x.FullName).IsRequired().HasMaxLength(100);
            builder.Property(x => x.Phone).HasMaxLength(20);
            builder.Property(x => x.AvatarUrl).HasMaxLength(500);
            builder.Property(x => x.Bio).HasMaxLength(500);
            builder.HasQueryFilter(x => !x.IsDeleted);

            builder.HasOne(x => x.Tenant)
                   .WithMany(x => x.Staff)
                   .HasForeignKey(x => x.TenantId)
                   .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Services)
                   .WithMany(x => x.Staff)
                   .UsingEntity(j => j.ToTable("StaffServices"));
        }
    }
}
