using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Auth.Handlers;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using BerberApp.Tests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;

namespace BerberApp.Tests.Auth;

public class LoginHandlerTests
{
    private static readonly string HashedPassword = BCrypt.Net.BCrypt.HashPassword("Test1234!");

    private static Tenant SeedTenant(BerberApp.Infrastructure.Persistence.AppDbContext db)
    {
        var tenant = new Tenant { Id = Guid.NewGuid(), Name = "Test Salon", Subdomain = "test-salon", IsActive = true };
        db.Tenants.Add(tenant);
        return tenant;
    }

    private static User SeedUser(BerberApp.Infrastructure.Persistence.AppDbContext db, Guid tenantId, string email = "test@example.com")
    {
        var user = new User
        {
            Id           = Guid.NewGuid(),
            TenantId     = tenantId,
            Email        = email,
            PasswordHash = HashedPassword,
            FirstName    = "Ali",
            LastName     = "Veli",
            Role         = UserRole.Admin,
            IsVerified   = true
        };
        db.Users.Add(user);
        return user;
    }

    private static Subscription SeedTrialSubscription(BerberApp.Infrastructure.Persistence.AppDbContext db, Guid tenantId)
    {
        var sub = new Subscription
        {
            Id         = Guid.NewGuid(),
            TenantId   = tenantId,
            Plan       = PlanType.Baslangic,
            Status     = SubscriptionStatus.Trial,
            StartDate  = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            Price      = 0,
            Currency   = "TRY"
        };
        db.Subscriptions.Add(sub);
        return sub;
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsTokenAndUserInfo()
    {
        // Arrange
        using var db = TestDatabase.Create();
        var tenant = SeedTenant(db);
        var user   = SeedUser(db, tenant.Id);
        SeedTrialSubscription(db, tenant.Id);
        await db.SaveChangesAsync();

        var handler = new LoginHandler(db, new FakeJwtService(), NullLogger<LoginHandler>.Instance);

        // Act
        var result = await handler.Handle(new LoginCommand { Email = "test@example.com", Password = "Test1234!" }, default);

        // Assert
        result.AccessToken.Should().Be("fake-access-token");
        result.RefreshToken.Should().Be("fake-refresh-token");
        result.Email.Should().Be("test@example.com");
        result.FullName.Should().Be("Ali Veli");
        result.IsOnTrial.Should().BeTrue();
    }

    [Fact]
    public async Task Login_WrongPassword_ThrowsUnauthorized()
    {
        // Arrange
        using var db = TestDatabase.Create();
        var tenant = SeedTenant(db);
        SeedUser(db, tenant.Id);
        await db.SaveChangesAsync();

        var handler = new LoginHandler(db, new FakeJwtService(), NullLogger<LoginHandler>.Instance);

        // Act
        var act = async () => await handler.Handle(new LoginCommand { Email = "test@example.com", Password = "WrongPass!" }, default);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Login_NonExistentEmail_ThrowsUnauthorized()
    {
        // Arrange — empty DB, no users
        using var db = TestDatabase.Create();
        await db.SaveChangesAsync();

        var handler = new LoginHandler(db, new FakeJwtService(), NullLogger<LoginHandler>.Instance);

        // Act
        var act = async () => await handler.Handle(new LoginCommand { Email = "nobody@example.com", Password = "Test1234!" }, default);

        // Assert — must NOT reveal whether user exists
        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*Email veya şifre hatalı*");
    }

    [Fact]
    public async Task Login_WithActiveSubscription_ReturnsPlanInResponse()
    {
        // Arrange
        using var db = TestDatabase.Create();
        var tenant = SeedTenant(db);
        SeedUser(db, tenant.Id);
        db.Subscriptions.Add(new Subscription
        {
            TenantId   = tenant.Id,
            Plan       = PlanType.Profesyonel,
            Status     = SubscriptionStatus.Active,
            StartDate  = DateTime.UtcNow.AddDays(-5),
            ExpiryDate = DateTime.UtcNow.AddDays(25),
            Price      = 1799m,
            Currency   = "TRY"
        });
        await db.SaveChangesAsync();

        var handler = new LoginHandler(db, new FakeJwtService(), NullLogger<LoginHandler>.Instance);

        // Act
        var result = await handler.Handle(new LoginCommand { Email = "test@example.com", Password = "Test1234!" }, default);

        // Assert
        result.IsOnTrial.Should().BeFalse();
        result.SubscriptionExpired.Should().BeFalse();
    }

    [Fact]
    public async Task Login_NoSubscription_AutoCreatesTrialAndReturnsToken()
    {
        // Arrange — user with no subscription
        using var db = TestDatabase.Create();
        var tenant = SeedTenant(db);
        SeedUser(db, tenant.Id);
        await db.SaveChangesAsync();

        var handler = new LoginHandler(db, new FakeJwtService(), NullLogger<LoginHandler>.Instance);

        // Act
        var result = await handler.Handle(new LoginCommand { Email = "test@example.com", Password = "Test1234!" }, default);

        // Assert — trial auto-created
        result.IsOnTrial.Should().BeTrue();
        result.TrialEndsAt.Should().NotBeNull();
        db.Subscriptions.Count().Should().Be(1);
    }
}
