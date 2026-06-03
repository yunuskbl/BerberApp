using BerberApp.Api.Authorization;
using BerberApp.Api.Middleware;
using BerberApp.Application.Common.Behaviors;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Common.Services;
using BerberApp.Infrastructure.Identity;
using BerberApp.Infrastructure.Jobs;
using BerberApp.Infrastructure.Persistence;
using BerberApp.Infrastructure.Persistence.Repositories;
using BerberApp.Infrastructure.Services;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;

AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(o =>
        o.JsonSerializerOptions.Converters.Add(
            new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "JWT token girin. Örnek: eyJhbGci..."
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});
builder.Services.AddMemoryCache();

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IAppDbContext>(provider =>
    provider.GetRequiredService<AppDbContext>());

// Generic Repository
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(BerberApp.Application.Common.Interfaces.IAppDbContext).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(BerberApp.Application.Common.Interfaces.IAppDbContext).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

// JWT
var jwtKey = builder.Configuration["Jwt:Key"]!;
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// Services
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IWhatsAppService, WhatsAppService>();
builder.Services.AddScoped<SmsService>();
builder.Services.AddHttpClient<ISmsService, IletimerkeziSmsService>();
builder.Services.AddScoped<INotificationService, LinkNotificationService>();
builder.Services.AddScoped<IPlanService, PlanService>();
builder.Services.AddScoped<IIyzicoService, BerberApp.Infrastructure.Services.IyzicoService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddSingleton<IAppSettings, AppSettingsService>();
builder.Services.AddScoped<BerberApp.Infrastructure.Services.IyzicoService>();
builder.Services.AddHttpClient<ITranslationService, MyMemoryTranslationService>();
builder.Services.AddHttpClient<IExchangeRateService, FrankfurterExchangeRateService>();

// Hangfire
builder.Services.AddHangfire(config =>
    config.UsePostgreSqlStorage(c =>
        c.UseNpgsqlConnection(
            builder.Configuration.GetConnectionString("DefaultConnection"))));
builder.Services.AddHangfireServer();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        var allowedOrigins = new List<string>
        {
            "https://ayarliyo.com",
            "http://ayarliyo.com",
            "https://www.ayarliyo.com",
            "http://www.ayarliyo.com"
        };

        if (!builder.Environment.IsProduction())
        {
            allowedOrigins.AddRange([
                "http://localhost:4200",
                "https://localhost:4200",
                "http://localhost:80",
                "http://berberapp-admin",
                "https://bless-overcoat-duct.ngrok-free.dev"
            ]);
        }

        policy.WithOrigins([.. allowedOrigins])
              .WithHeaders("Authorization", "Content-Type", "Accept")
              .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS");
    });
});

var app = builder.Build();

// HSTS — sadece doğrudan HTTPS sunuluyorsa (nginx/ngrok arkasında gerekmez)
if (app.Environment.IsProduction() && !app.Environment.IsEnvironment("Container"))
{
    app.UseHsts();
}

// Swagger — production'da SuperAdmin korumasıyla, diğer ortamlarda açık
app.UseSwagger();
if (app.Environment.IsProduction())
{
    app.UseMiddleware<SwaggerAuthMiddleware>();
}
app.UseSwaggerUI();

// Static Files — auth'tan önce, en başta olmalı
var staticFileOptions = new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = ""
};
app.UseStaticFiles(staticFileOptions);

// Middleware
app.UseMiddleware<SecurityAuditMiddleware>();    // En dışta — 401/403/429 yanıtlarını loglar
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();
app.UseMiddleware<BranchContextMiddleware>();

// Hangfire Dashboard — production'da sadece SuperAdmin erişebilir
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = app.Environment.IsProduction()
        ? [new HangfireAuthFilter()]
        : [new Hangfire.Dashboard.LocalRequestsOnlyAuthorizationFilter()]
});

// Migration + Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var env = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
    db.Database.Migrate();

    // StaffDaysOff tablosu yoksa oluştur (boş migration'dan kaynaklanan eksiklik)
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""StaffDaysOff"" (
            ""Id""        uuid NOT NULL,
            ""StaffId""   uuid NOT NULL,
            ""Date""      date NOT NULL,
            ""Reason""    text,
            ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT now(),
            ""UpdatedAt"" timestamp without time zone,
            ""IsDeleted"" boolean NOT NULL DEFAULT false,
            CONSTRAINT ""PK_StaffDaysOff"" PRIMARY KEY (""Id""),
            CONSTRAINT ""FK_StaffDaysOff_Staff_StaffId"" FOREIGN KEY (""StaffId"")
                REFERENCES ""Staff"" (""Id"") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ""IX_StaffDaysOff_StaffId"" ON ""StaffDaysOff"" (""StaffId"");
    ");

    // TenantClosures tablosu yoksa oluştur
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""TenantClosures"" (
            ""Id""        uuid NOT NULL,
            ""TenantId""  uuid NOT NULL,
            ""StartDate"" date NOT NULL,
            ""EndDate""   date NOT NULL,
            ""Reason""    text,
            ""CreatedAt"" timestamp without time zone NOT NULL DEFAULT now(),
            ""UpdatedAt"" timestamp without time zone,
            ""IsDeleted"" boolean NOT NULL DEFAULT false,
            CONSTRAINT ""PK_TenantClosures"" PRIMARY KEY (""Id""),
            CONSTRAINT ""FK_TenantClosures_Tenants_TenantId"" FOREIGN KEY (""TenantId"")
                REFERENCES ""Tenants"" (""Id"") ON DELETE CASCADE
        );
        CREATE INDEX IF NOT EXISTS ""IX_TenantClosures_TenantId"" ON ""TenantClosures"" (""TenantId"");
    ");

    // Users tablosuna StaffId kolonu ekle (idempotent)
    db.Database.ExecuteSqlRaw(@"
        ALTER TABLE ""Users"" ADD COLUMN IF NOT EXISTS ""StaffId"" uuid;
    ");

    // Appointments tablosuna reminder flag kolonlarını ekle (idempotent)
    db.Database.ExecuteSqlRaw(@"
        ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""ReminderSent24h"" boolean NOT NULL DEFAULT false;
        ALTER TABLE ""Appointments"" ADD COLUMN IF NOT EXISTS ""ReminderSent1h""  boolean NOT NULL DEFAULT false;
    ");

    // Expenses tablosu (gelir/gider takibi)
    db.Database.ExecuteSqlRaw(@"
        CREATE TABLE IF NOT EXISTS ""Expenses"" (
            ""Id""          uuid NOT NULL DEFAULT gen_random_uuid(),
            ""TenantId""    uuid NOT NULL,
            ""Date""        timestamp with time zone NOT NULL,
            ""Amount""      numeric(18,2) NOT NULL,
            ""Currency""    varchar(10) NOT NULL DEFAULT 'TRY',
            ""Category""    varchar(100) NOT NULL DEFAULT '',
            ""Description"" varchar(500),
            ""Note""        varchar(1000),
            ""CreatedAt""   timestamp with time zone NOT NULL DEFAULT now(),
            ""UpdatedAt""   timestamp with time zone,
            ""IsDeleted""   boolean NOT NULL DEFAULT false,
            CONSTRAINT ""PK_Expenses"" PRIMARY KEY (""Id"")
        );
        CREATE INDEX IF NOT EXISTS ""IX_Expenses_TenantId"" ON ""Expenses"" (""TenantId"");
        CREATE INDEX IF NOT EXISTS ""IX_Expenses_Date"" ON ""Expenses"" (""Date"");
    ");

    await SeedData.SeedAsync(db, env);
    await SeedData.SeedSuperAdminAsync(db);
}

// Hangfire Recurring Jobs
// Sabah 10:00 — yarın randevusu olan müşterilere gün sonu özet hatırlatması
RecurringJob.AddOrUpdate<AppointmentReminderJob>(
    "appointment-reminders",
    job => job.SendRemindersAsync(),
    "0 10 * * *"
);
// Her saat başı — randevudan tam 24 saat önce hatırlatma
RecurringJob.AddOrUpdate<AppointmentReminder24hJob>(
    "appointment-reminders-24h",
    job => job.SendRemindersAsync(),
    "0 * * * *"
);
// Her 15 dakikada bir — randevudan tam 1 saat önce hatırlatma
RecurringJob.AddOrUpdate<AppointmentReminder1hJob>(
    "appointment-reminders-1h",
    job => job.SendRemindersAsync(),
    "*/15 * * * *"
);
// Her 5 dakikada bir — süresi dolan pending randevuları iptal et
RecurringJob.AddOrUpdate<ExpireAppointmentsJob>(
    "expire-appointments",
    job => job.ExpireOldAppointmentsAsync(),
    "*/5 * * * *"
);
// Her gün sabah 09:00 — abonelik sona erme uyarısı
RecurringJob.AddOrUpdate<SubscriptionExpiryReminderJob>(
    "subscription-expiry-reminders",
    job => job.SendExpiryRemindersAsync(),
    "0 9 * * *"
);

app.MapControllers();

app.Run();