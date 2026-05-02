using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace BerberApp.Infrastructure.Persistence;

public static class SeedData
{
    // SuperAdmin'in ait olacağı sistem tenant ID'si (sabit)
    private static readonly Guid SYSTEM_TENANT_ID = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public static async Task SeedAsync(AppDbContext context, IHostEnvironment env)
    {
        // Demo veri sadece development ortamında eklenir
        if (!env.IsDevelopment()) return;
        if (await context.Tenants.AnyAsync()) return;

        // Tenant
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = "Yunus Berber Salonu",
            Subdomain = "yunus",
            Phone = "05551234567",
            Address = "Atatürk Cad. No:1 Bursa",
            IsActive = true
        };
        context.Tenants.Add(tenant);

        // Admin User
        var adminUser = new User
        {
            TenantId = tenant.Id,
            Email = "admin@berber.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
            FirstName = "Yunus",
            LastName = "Emre",
            Phone = "05551234567",
            Role = UserRole.Admin,
            IsVerified = true
        };
        context.Users.Add(adminUser);

        // Personel
        var staff1 = new Staff
        {
            TenantId = tenant.Id,
            FullName = "Ahmet Usta",
            Phone = "05559876543",
            Bio = "15 yıllık deneyimli berber",
            IsActive = true
        };

        var staff2 = new Staff
        {
            TenantId = tenant.Id,
            FullName = "Mehmet Çavuş",
            Phone = "05558765432",
            Bio = "Saç tasarım uzmanı",
            IsActive = true
        };

        context.Staff.AddRange(staff1, staff2);

        // Hizmetler
        var service1 = new Service
        {
            TenantId = tenant.Id,
            Name = "Saç Kesimi",
            DurationMinutes = 30,
            Price = 150,
            Currency = "TRY",
            Color = "#7c3aed",
            IsActive = true
        };

        var service2 = new Service
        {
            TenantId = tenant.Id,
            Name = "Sakal Tıraşı",
            DurationMinutes = 20,
            Price = 100,
            Currency = "TRY",
            Color = "#2563eb",
            IsActive = true
        };

        var service3 = new Service
        {
            TenantId = tenant.Id,
            Name = "Saç + Sakal",
            DurationMinutes = 45,
            Price = 220,
            Currency = "TRY",
            Color = "#16a34a",
            IsActive = true
        };

        var service4 = new Service
        {
            TenantId = tenant.Id,
            Name = "Çocuk Kesimi",
            DurationMinutes = 20,
            Price = 80,
            Currency = "TRY",
            Color = "#d97706",
            IsActive = true
        };

        context.Services.AddRange(service1, service2, service3, service4);

        // Çalışma Saatleri — Staff1
        var workingDays = new[] {
            DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday,
            DayOfWeek.Thursday, DayOfWeek.Friday, DayOfWeek.Saturday
        };

        foreach (var day in workingDays)
        {
            context.WorkingHours.Add(new WorkingHour
            {
                StaffId = staff1.Id,
                DayOfWeek = day,
                StartTime = new TimeOnly(9, 0),
                EndTime = new TimeOnly(18, 0),
                IsOff = false
            });

            context.WorkingHours.Add(new WorkingHour
            {
                StaffId = staff2.Id,
                DayOfWeek = day,
                StartTime = new TimeOnly(10, 0),
                EndTime = new TimeOnly(19, 0),
                IsOff = false
            });
        }

        // Pazar kapalı
        context.WorkingHours.Add(new WorkingHour
        {
            StaffId = staff1.Id,
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(9, 0),
            EndTime = new TimeOnly(18, 0),
            IsOff = true
        });

        context.WorkingHours.Add(new WorkingHour
        {
            StaffId = staff2.Id,
            DayOfWeek = DayOfWeek.Sunday,
            StartTime = new TimeOnly(10, 0),
            EndTime = new TimeOnly(19, 0),
            IsOff = true
        });

        // Müşteriler
        var customers = new List<Customer>
        {
            new() { TenantId = tenant.Id, FullName = "Ali Yılmaz",    Phone = "05551111111", Email = "ali@mail.com",    TotalVisits = 5  },
            new() { TenantId = tenant.Id, FullName = "Veli Kaya",     Phone = "05552222222", Email = "veli@mail.com",   TotalVisits = 3  },
            new() { TenantId = tenant.Id, FullName = "Hasan Demir",   Phone = "05553333333", Email = "hasan@mail.com",  TotalVisits = 8  },
            new() { TenantId = tenant.Id, FullName = "Kemal Şahin",   Phone = "05554444444", Email = "kemal@mail.com",  TotalVisits = 1  },
            new() { TenantId = tenant.Id, FullName = "Murat Çelik",   Phone = "05555555555", Email = "murat@mail.com",  TotalVisits = 12 },
            new() { TenantId = tenant.Id, FullName = "Osman Arslan",  Phone = "05556666666", Email = "osman@mail.com",  TotalVisits = 2  },
            new() { TenantId = tenant.Id, FullName = "Serkan Koç",    Phone = "05557777777", Email = "serkan@mail.com", TotalVisits = 7  },
            new() { TenantId = tenant.Id, FullName = "Burak Erdoğan", Phone = "05558888888", Email = "burak@mail.com",  TotalVisits = 4  },
        };
        context.Customers.AddRange(customers);

        await context.SaveChangesAsync();

        // Randevular — bugün ve yakın günler
        var today = DateTime.UtcNow.Date;
        var appointments = new List<Appointment>
        {
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[0].Id,
                StaffId    = staff1.Id,
                ServiceId  = service1.Id,
                StartTime  = today.AddHours(9),
                EndTime    = today.AddHours(9).AddMinutes(30),
                Status     = AppointmentStatus.Confirmed,
                Notes      = "İlk randevu"
            },
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[1].Id,
                StaffId    = staff1.Id,
                ServiceId  = service2.Id,
                StartTime  = today.AddHours(10),
                EndTime    = today.AddHours(10).AddMinutes(20),
                Status     = AppointmentStatus.Confirmed
            },
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[2].Id,
                StaffId    = staff2.Id,
                ServiceId  = service3.Id,
                StartTime  = today.AddHours(11),
                EndTime    = today.AddHours(11).AddMinutes(45),
                Status     = AppointmentStatus.Completed
            },
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[3].Id,
                StaffId    = staff1.Id,
                ServiceId  = service4.Id,
                StartTime  = today.AddHours(14),
                EndTime    = today.AddHours(14).AddMinutes(20),
                Status     = AppointmentStatus.Pending
            },
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[4].Id,
                StaffId    = staff2.Id,
                ServiceId  = service1.Id,
                StartTime  = today.AddDays(1).AddHours(9),
                EndTime    = today.AddDays(1).AddHours(9).AddMinutes(30),
                Status     = AppointmentStatus.Confirmed
            },
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[5].Id,
                StaffId    = staff1.Id,
                ServiceId  = service2.Id,
                StartTime  = today.AddDays(1).AddHours(11),
                EndTime    = today.AddDays(1).AddHours(11).AddMinutes(20),
                Status     = AppointmentStatus.Confirmed
            },
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[6].Id,
                StaffId    = staff2.Id,
                ServiceId  = service3.Id,
                StartTime  = today.AddDays(-1).AddHours(10),
                EndTime    = today.AddDays(-1).AddHours(10).AddMinutes(45),
                Status     = AppointmentStatus.Completed
            },
            new()
            {
                TenantId   = tenant.Id,
                CustomerId = customers[7].Id,
                StaffId    = staff1.Id,
                ServiceId  = service1.Id,
                StartTime  = today.AddDays(-1).AddHours(14),
                EndTime    = today.AddDays(-1).AddHours(14).AddMinutes(30),
                Status     = AppointmentStatus.Cancelled
            },
        };

        context.Appointments.AddRange(appointments);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// SuperAdmin kullanıcısını seed'le (her zaman çalışır, sadece SuperAdmin yoksa oluşturur)
    /// </summary>
    public static async Task SeedSuperAdminAsync(AppDbContext context)
    {
        // Sistem tenant'ı oluştur (yok ise)
        var systemTenant = await context.Tenants.FirstOrDefaultAsync(t => t.Id == SYSTEM_TENANT_ID);
        if (systemTenant == null)
        {
            systemTenant = new Tenant
            {
                Id = SYSTEM_TENANT_ID,
                Name = "BerberApp System",
                Subdomain = "system",
                IsActive = false // Listelemede görünmeyecek
            };
            context.Tenants.Add(systemTenant);
            await context.SaveChangesAsync();
        }

        // SuperAdmin kullanıcısını oluştur (yok ise)
        var superAdminExists = await context.Users
            .IgnoreQueryFilters()
            .AnyAsync(u => u.Role == UserRole.SuperAdmin);

        if (!superAdminExists)
        {
            var password = Environment.GetEnvironmentVariable("SUPERADMIN_PASSWORD")
                ?? throw new InvalidOperationException("SUPERADMIN_PASSWORD environment variable is not set.");

            var superAdmin = new User
            {
                TenantId = SYSTEM_TENANT_ID,
                Email = "superadmin@berberapp.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                FirstName = "Super",
                LastName = "Admin",
                Role = UserRole.SuperAdmin,
                IsVerified = true
            };
            context.Users.Add(superAdmin);
            await context.SaveChangesAsync();
        }
    }
}