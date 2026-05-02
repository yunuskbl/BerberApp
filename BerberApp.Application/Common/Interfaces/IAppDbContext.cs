using BerberApp.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Common.Interfaces
{
    public interface IAppDbContext
    {
        DbSet<TenantEntity> Tenants { get; }
        DbSet<UserEntity> Users { get; }
        DbSet<StaffEntity> Staff { get; }
        DbSet<ServiceEntity> Services { get; }
        DbSet<CustomerEntity> Customers { get; }
        DbSet<AppointmentEntity> Appointments { get; }
        DbSet<WorkingHourEntity> WorkingHours { get; }
        DbSet<NotificationEntity> Notifications { get; }
        DbSet<Subscription> Subscriptions { get; }
        DbSet<TenantPhotoEntity> TenantPhotos { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
