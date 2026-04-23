using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Entities;
using BerberApp.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BerberApp.Application.Common.Services;

/// <summary>
/// Plan management service
/// </summary>
public interface IPlanService
{
    Task<PlanType?> GetUserPlanAsync(Guid tenantId, CancellationToken ct = default);
    Task<bool> HasAccessToFeatureAsync(Guid tenantId, PlanType requiredPlan, CancellationToken ct = default);
    Task<Subscription?> GetActiveSubscriptionAsync(Guid tenantId, CancellationToken ct = default);
}

public class PlanService : IPlanService
{
    private readonly IAppDbContext _context;

    public PlanService(IAppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Get user's current plan
    /// </summary>
    public async Task<PlanType?> GetUserPlanAsync(Guid tenantId, CancellationToken ct = default)
    {
        var subscription = await _context.Subscriptions
            .Where(x => x.TenantId == tenantId &&
                        x.Status == SubscriptionStatus.Active &&
                        x.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(ct);

        return subscription?.Plan;
    }

    /// <summary>
    /// Check if user has access to feature
    /// </summary>
    public async Task<bool> HasAccessToFeatureAsync(
        Guid tenantId,
        PlanType requiredPlan,
        CancellationToken ct = default)
    {
        var userPlan = await GetUserPlanAsync(tenantId, ct);

        if (userPlan == null)
            return false;

        // Basic can access only Basic
        // Standard can access Basic + Standard
        // Full can access all
        return userPlan >= requiredPlan;
    }

    /// <summary>
    /// Get active subscription
    /// </summary>
    public async Task<Subscription?> GetActiveSubscriptionAsync(Guid tenantId, CancellationToken ct = default)
    {
        return await _context.Subscriptions
            .Where(x => x.TenantId == tenantId &&
                        x.Status == SubscriptionStatus.Active &&
                        x.ExpiryDate > DateTime.UtcNow)
            .OrderByDescending(x => x.StartDate)
            .FirstOrDefaultAsync(ct);
    }
}