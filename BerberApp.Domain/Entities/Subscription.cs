using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BerberApp.Domain.Common;
using BerberApp.Domain.Enums;

namespace BerberApp.Domain.Entities;

/// <summary>
/// Tenant subscription management
/// </summary>
public class Subscription : BaseEntity
{
    public Guid TenantId { get; set; }

    public PlanType Plan { get; set; }

    public DateTime StartDate { get; set; }

    public DateTime ExpiryDate { get; set; }

    public SubscriptionStatus Status { get; set; }

    public decimal Price { get; set; }

    public string Currency { get; set; } = "TRY";

    public bool IsAutoRenewal { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;

    public bool IsActive => Status == SubscriptionStatus.Active && DateTime.UtcNow < ExpiryDate;

    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;
}
