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
builder.Services.AddControllers();
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
builder.Services.AddHttpClient<SmsService>();
builder.Services.AddScoped<ISmsService, SmsService>();
builder.Services.AddScoped<INotificationService, LinkNotificationService>();
builder.Services.AddScoped<IPlanService, PlanService>();

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
            "https://berberapp.com.tr",
            "http://berberapp.com.tr"
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

// Swagger — sadece development'ta
if (!app.Environment.IsProduction())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Static Files — auth'tan önce, en başta olmalı
var staticFileOptions = new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(Directory.GetCurrentDirectory(), "wwwroot")),
    RequestPath = ""
};
app.UseStaticFiles(staticFileOptions);

// Middleware
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();

app.UseCors("AllowAngular");
app.UseAuthentication();
app.UseAuthorization();

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
    db.Database.Migrate();
    await SeedData.SeedAsync(db);
    await SeedData.SeedSuperAdminAsync(db);
}

// Hangfire Recurring Job
RecurringJob.AddOrUpdate<AppointmentReminderJob>(
    "appointment-reminders",
    job => job.SendRemindersAsync(),
    "0 10 * * *"
);
// Her 5 dakikada bir kontrol et
RecurringJob.AddOrUpdate<ExpireAppointmentsJob>(
    "expire-appointments",
    job => job.ExpireOldAppointmentsAsync(),
    "*/5 * * * *"
);

app.MapControllers();

app.Run();