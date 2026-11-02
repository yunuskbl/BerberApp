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

    // Iyzico payment tracking
    public string? IyzicoPaymentId { get; set; }
    public string? IyzicoConversationId { get; set; }
    public string? CardToken { get; set; }
    public string? CardUserKey { get; set; }

    // Navigation
    public virtual Tenant Tenant { get; set; } = null!;

    public bool IsActive => Status == SubscriptionStatus.Active && DateTime.UtcNow < ExpiryDate;

    public bool IsExpired => DateTime.UtcNow >= ExpiryDate;

    /// <summary>
    /// Aboneliğin fiilen verdiği haklar.
    ///
    /// Deneme süresince <see cref="PlanType.Profesyonel"/> geçerli: deneme
    /// kaydı <see cref="PlanType.Baslangic"/> olarak açılıyordu ve bu, işletmenin
    /// tam da "ödeyeyim mi" kararını verdiği 30 gün boyunca raporları (PlanGuard
    /// Profesyonel istiyor) ve beşinci personele kadar ekleme hakkını kilitliyordu.
    /// Ödeyeceği şeyi göremeyen denemeden ödeme çıkmaz.
    ///
    /// Hak <b>durumdan türetiliyor, saklanmıyor</b>: deneme satırındaki
    /// <see cref="Plan"/> alanı Baslangic kalıyor, böylece o satır bir gün
    /// ödeme almadan Active'e çevrilirse Profesyonel bedava verilmiş olmuyor.
    /// Süre dolduğunda hak kendiliğinden düşüyor.
    /// </summary>
    public PlanType EffectivePlan =>
        Status == SubscriptionStatus.Trial && !IsExpired
            ? PlanType.Profesyonel
            : Plan;
}
