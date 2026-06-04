using BerberApp.Application.Auth.Commands;
using BerberApp.Application.Auth.Handlers;
using BerberApp.Application.Common.Exceptions;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BerberApp.Tests.Auth;

public class RegisterHandlerTests
{
    private static readonly RegisterCommand ValidCommand = new()
    {
        TenantName = "Ahmet Berberi",
        Subdomain  = "ahmet-berberi",
        FirstName  = "Ahmet",
        LastName   = "Yıldız",
        Email      = "ahmet@example.com",
        Password   = "Ahmet1234!",
        Phone      = "+905551234567"
    };

    private static Mock<IAppSettings> AppSettingsMock()
    {
        var m = new Mock<IAppSettings>();
        m.Setup(x => x.FrontendBaseUrl).Returns("https://ayarliyo.com");
        return m;
    }

    private static void SeedVerifiedOtp(BerberApp.Infrastructure.Persistence.AppDbContext db, string phone)
    {
        db.OtpRecords.Add(new OtpRecord
        {
            Id         = Guid.NewGuid(),
            Phone      = phone,
            Code       = "123456",
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow.AddMinutes(-5),
            ExpiresAt  = DateTime.UtcNow.AddMinutes(25),
            CreatedAt  = DateTime.UtcNow.AddMinutes(-6)
        });
    }

    [Fact]
    public async Task Register_ValidRequest_CreatesTenantUserAndTrialSubscription()
    {
        // Arrange
        using var db = TestDatabase.Create();
        SeedVerifiedOtp(db, ValidCommand.Phone!);
        await db.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        var handler   = new RegisterHandler(db, new FakeJwtService(), emailMock.Object, AppSettingsMock().Object);

        // Act
        var result = await handler.Handle(ValidCommand, default);

        // Assert
        result.AccessToken.Should().Be("fake-access-token");
        result.Subdomain.Should().Be("ahmet-berberi");
        result.IsOnTrial.Should().BeTrue();
        result.TrialEndsAt.Should().BeCloseTo(DateTime.UtcNow.AddDays(30), precision: TimeSpan.FromSeconds(5));

        db.Tenants.Count().Should().Be(1);
        db.Users.Count().Should().Be(1);
        db.Subscriptions.Count().Should().Be(1);
    }

    [Fact]
    public async Task Register_WithoutVerifiedOtp_ThrowsBadRequest()
    {
        // Arrange — no OtpRecord seeded
        using var db = TestDatabase.Create();
        var emailMock = new Mock<IEmailService>();
        var handler   = new RegisterHandler(db, new FakeJwtService(), emailMock.Object, AppSettingsMock().Object);

        // Act
        var act = async () => await handler.Handle(ValidCommand, default);

        // Assert
        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*telefon numaranızı doğrulayın*");
    }

    [Fact]
    public async Task Register_DuplicateSubdomain_ThrowsConflict()
    {
        // Arrange
        using var db = TestDatabase.Create();
        SeedVerifiedOtp(db, ValidCommand.Phone!);
        db.Tenants.Add(new Tenant { Id = Guid.NewGuid(), Name = "Mevcut Salon", Subdomain = "ahmet-berberi", IsActive = true });
        await db.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        var handler   = new RegisterHandler(db, new FakeJwtService(), emailMock.Object, AppSettingsMock().Object);

        // Act
        var act = async () => await handler.Handle(ValidCommand, default);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*ahmet-berberi*");
    }

    [Fact]
    public async Task Register_DuplicateEmail_ThrowsConflict()
    {
        // Arrange
        using var db = TestDatabase.Create();
        SeedVerifiedOtp(db, ValidCommand.Phone!);
        var existingTenant = new Tenant { Id = Guid.NewGuid(), Name = "Başka Salon", Subdomain = "baska-salon", IsActive = true };
        db.Tenants.Add(existingTenant);
        db.Users.Add(new User
        {
            TenantId     = existingTenant.Id,
            Email        = "ahmet@example.com",
            PasswordHash = "hash",
            FirstName    = "Başka",
            LastName     = "Kişi"
        });
        await db.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        var handler   = new RegisterHandler(db, new FakeJwtService(), emailMock.Object, AppSettingsMock().Object);

        // Act
        var act = async () => await handler.Handle(ValidCommand, default);

        // Assert
        await act.Should().ThrowAsync<ConflictException>()
            .WithMessage("*ahmet@example.com*");
    }

    [Fact]
    public async Task Register_Success_SendsBothVerificationAndWelcomeEmail()
    {
        // Arrange
        using var db = TestDatabase.Create();
        SeedVerifiedOtp(db, ValidCommand.Phone!);
        await db.SaveChangesAsync();

        var emailMock = new Mock<IEmailService>();
        emailMock.Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);
        emailMock.Setup(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                 .Returns(Task.CompletedTask);

        var handler = new RegisterHandler(db, new FakeJwtService(), emailMock.Object, AppSettingsMock().Object);

        // Act
        await handler.Handle(ValidCommand, default);

        // Wait for fire-and-forget emails
        await Task.Delay(100);

        // Assert — her iki email de gönderilmeli
        emailMock.Verify(x => x.SendEmailVerificationAsync("ahmet@example.com", "Ahmet Yıldız", It.Is<string>(u => u.Contains("email-dogrula"))), Times.Once);
        emailMock.Verify(x => x.SendWelcomeEmailAsync("ahmet@example.com", "Ahmet Yıldız", "Ahmet Berberi"), Times.Once);
    }
}
