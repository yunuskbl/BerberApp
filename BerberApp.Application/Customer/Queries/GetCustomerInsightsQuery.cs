using MediatR;

namespace BerberApp.Application.Customer.Queries;

/// <summary>
/// Müşteri takibi raporu: dönem içi hareket (KPI'lar, yeni/tekrar gelen zaman
/// serisi, en çok harcayanlar) ve döneme bağlı olmayan iki kesit (sadakat
/// dağılımı ve kayıp riski).
///
/// "Ziyaret" her yerde <b>tamamlanmış</b> randevu demek — gelir raporunun
/// saydığı şeyle aynı. İptal ve gelmedi sayılmıyor, yoksa iki rapor aynı
/// müşteri için farklı sayı gösterirdi.
/// </summary>
public class GetCustomerInsightsQuery : IRequest<CustomerInsightsDto>
{
    public Guid TenantId { get; set; }
    public List<Guid>? TenantIds { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    /// <summary>Kaç gün gelmeyen müşteri "kayıp riski" sayılsın.</summary>
    public int AtRiskAfterDays { get; set; } = 60;
}

public class CustomerInsightsDto
{
    // ── Seçili dönem ────────────────────────────────────────────────────────
    /// <summary>Dönemde en az bir kez gelen farklı müşteri sayısı.</summary>
    public int ActiveCustomers { get; set; }
    /// <summary>Aynı uzunluktaki bir önceki dönemde gelen farklı müşteri.</summary>
    public int PreviousActiveCustomers { get; set; }
    /// <summary>Bunların, hayatında ilk kez bu dönemde geleni.</summary>
    public int NewCustomers { get; set; }
    /// <summary>Bunların, daha önce de gelmiş olanı.</summary>
    public int ReturningCustomers { get; set; }
    /// <summary>Tekrar gelenlerin aktif müşterilere oranı (%).</summary>
    public decimal RepeatRate { get; set; }
    /// <summary>Dönem geliri / aktif müşteri (TRY).</summary>
    public decimal AverageSpendPerCustomer { get; set; }
    public decimal PeriodRevenue { get; set; }
    public List<CustomerTimelinePointDto> Timeline { get; set; } = new();
    public List<CustomerRankDto> TopCustomers { get; set; } = new();

    // ── Döneme bağlı olmayan kesitler ───────────────────────────────────────
    /// <summary>İşletmenin kayıtlı müşteri sayısı (silinmemiş).</summary>
    public int TotalCustomers { get; set; }
    /// <summary>Tüm zamanlarda en az bir kez gelmiş müşteri sayısı.</summary>
    public int EverVisitedCustomers { get; set; }
    /// <summary>
    /// En az iki kez gelenlerin ziyaretleri arasındaki ortalama gün.
    /// Tek ziyaretlilerde aralık tanımsız olduğu için onlar dışarıda.
    /// </summary>
    public int AverageVisitGapDays { get; set; }
    public List<LoyaltyBucketDto> LoyaltyBuckets { get; set; } = new();
    public List<AtRiskCustomerDto> AtRisk { get; set; } = new();
    public int AtRiskCount { get; set; }
    /// <summary>Kayıp riskindekilerin kaçına kampanya mesajı gönderilebilir.</summary>
    public int AtRiskWithConsent { get; set; }
    public int AtRiskAfterDays { get; set; }
}

public class CustomerTimelinePointDto
{
    public DateTime Date { get; set; }
    /// <summary>O gün gelenlerden ilk kez gelenler.</summary>
    public int NewCount { get; set; }
    /// <summary>O gün gelenlerden daha önce de gelmiş olanlar.</summary>
    public int ReturningCount { get; set; }
}

public class LoyaltyBucketDto
{
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    /// <summary>Sıralama için alt sınır; grafikte kovaların sırası bozulmasın.</summary>
    public int Order { get; set; }
}

public class CustomerRankDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public decimal TotalSpend { get; set; }
    public DateTime? LastVisit { get; set; }
}

public class AtRiskCustomerDto
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public int VisitCount { get; set; }
    public decimal TotalSpend { get; set; }
    public DateTime? LastVisit { get; set; }
    public int DaysSinceLastVisit { get; set; }
    /// <summary>
    /// Kampanya mesajı için açık rıza var mı. Rızası olmayana ticari ileti
    /// gönderilemez; liste bunu gizlemek yerine açıkça gösteriyor.
    /// </summary>
    public bool HasMarketingConsent { get; set; }
}
