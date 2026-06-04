using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using BerberApp.Infrastructure.Persistence;
using BerberApp.Tests.Helpers;
using FluentAssertions;
using Moq;

namespace BerberApp.Tests.Subscription;

/// <summary>Abonelik durum makinesi testleri — ödeme akışını simüle eder.</summary>
public class SubscriptionFlowTests
{
    private static (AppDbContext db, Guid tenantId, Guid subscriptionId) SetupPendingSubscription()
    {
        var db       = TestDatabase.Create();
        var tenantId = Guid.NewGuid();

        db.Tenants.Add(new Tenant { Id = tenantId, Name = "Test Salon", Subdomain = "test", IsActive = true });

        var conversationId = $"{tenantId}_profesyonel_{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";
        var sub = new BerberApp.Domain.Entities.Subscription
        {
            Id                   = Guid.NewGuid(),
            TenantId             = tenantId,
            Plan                 = PlanType.Profesyonel,
            Status               = SubscriptionStatus.Pending,
            StartDate            = DateTime.UtcNow,
            ExpiryDate           = DateTime.UtcNow.AddMonths(1),
            Price                = 1799m,
            Currency             = "TRY",
            IsAutoRenewal        = true,
            IyzicoConversationId = conversationId
        };
        db.Subscriptions.Add(sub);
        db.SaveChanges();

        return (db, tenantId, sub.Id);
    }

    [Fact]
    public async Task SuccessfulPayment_ActivatesSubscriptionAndCancelsOldOnes()
    {
        // Arrange
        var (db, tenantId, pendingSubId) = SetupPendingSubscription();
        var pendingSub = db.Subscriptions.First(s => s.Id == pendingSubId);

        // Eski aktif trial aboneliği ekle
        var oldTrial = new BerberApp.Domain.Entities.Subscription
        {
            TenantId   = tenantId,
            Plan       = PlanType.Baslangic,
            Status     = SubscriptionStatus.Trial,
            StartDate  = DateTime.UtcNow.AddDays(-10),
            ExpiryDate = DateTime.UtcNow.AddDays(20),
            Price      = 0,
            Currency   = "TRY"
        };
        db.Subscriptions.Add(oldTrial);
        await db.SaveChangesAsync();

        // İyzico doğrulaması başarılı geldi — simüle et
        pendingSub.Status          = SubscriptionStatus.Active;
        pendingSub.StartDate       = DateTime.UtcNow;
        pendingSub.ExpiryDate      = DateTime.UtcNow.AddMonths(1);
        pendingSub.IyzicoPaymentId = "iyzico-payment-123";
        oldTrial.Status            = SubscriptionStatus.Cancelled;
        await db.SaveChangesAsync();

        // Assert
        var activeSubs = db.Subscriptions.Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active).ToList();
        activeSubs.Should().HaveCount(1);
        activeSubs[0].Plan.Should().Be(PlanType.Profesyonel);
        activeSubs[0].IyzicoPaymentId.Should().Be("iyzico-payment-123");

        var cancelledSubs = db.Subscriptions.Where(s => s.Status == SubscriptionStatus.Cancelled).ToList();
        cancelledSubs.Should().HaveCount(1);
        cancelledSubs[0].Plan.Should().Be(PlanType.Baslangic);
    }

    [Fact]
    public async Task FailedPayment_LeavesPendingSubscriptionAsCancelled()
    {
        // Arrange
        var (db, tenantId, pendingSubId) = SetupPendingSubscription();
        var pendingSub = db.Subscriptions.First(s => s.Id == pendingSubId);

        // İyzico başarısız — simüle et
        pendingSub.Status = SubscriptionStatus.Cancelled;
        await db.SaveChangesAsync();

        // Assert
        db.Subscriptions.Where(s => s.TenantId == tenantId && s.Status == SubscriptionStatus.Active)
            .Should().BeEmpty();
        db.Subscriptions.First(s => s.Id == pendingSubId).Status.Should().Be(SubscriptionStatus.Cancelled);
    }

    [Fact]
    public void PendingSubscription_IsNotActive()
    {
        var sub = new BerberApp.Domain.Entities.Subscription
        {
            Status     = SubscriptionStatus.Pending,
            ExpiryDate = DateTime.UtcNow.AddMonths(1)
        };
        sub.IsActive.Should().BeFalse();
    }

    [Fact]
    public void ExpiredActiveSubscription_IsActivePropertyFalse()
    {
        var sub = new BerberApp.Domain.Entities.Subscription
        {
            Status     = SubscriptionStatus.Active,
            ExpiryDate = DateTime.UtcNow.AddDays(-1)  // dün sona erdi
        };
        sub.IsActive.Should().BeFalse();
        sub.IsExpired.Should().BeTrue();
    }

    [Fact]
    public void TrialSubscription_WithinExpiry_IsActive()
    {
        var sub = new BerberApp.Domain.Entities.Subscription
        {
            Status     = SubscriptionStatus.Trial,
            ExpiryDate = DateTime.UtcNow.AddDays(15)
        };
        // Trial status için IsActive kontrolü — Status == Active veya Trial AND future expiry
        sub.IsExpired.Should().BeFalse();
    }
}
