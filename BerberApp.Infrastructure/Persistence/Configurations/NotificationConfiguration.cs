using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BerberApp.Infrastructure.Persistence.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Message).IsRequired().HasMaxLength(1000);
        builder.Property(x => x.Channel).IsRequired();
        builder.HasQueryFilter(x => !x.IsDeleted);

        builder.HasOne(x => x.Appointment)
               .WithMany(x => x.Notifications)
               .HasForeignKey(x => x.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);
    }
}
