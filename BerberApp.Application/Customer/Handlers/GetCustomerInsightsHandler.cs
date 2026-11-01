using BerberApp.Application.Common.Interfaces;
using BerberApp.Application.Customer.Queries;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Customer.Handlers;

public class GetCustomerInsightsHandler
    : IRequestHandler<GetCustomerInsightsQuery, CustomerInsightsDto>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IExchangeRateService _exchangeRates;

    public GetCustomerInsightsHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IExchangeRateService exchangeRates)
    {
        _appointmentRepo = appointmentRepo;
        _customerRepo    = customerRepo;
        _serviceRepo     = serviceRepo;
        _exchangeRates   = exchangeRates;
    }

    /// <summary>Müşteri başına toplanan tüm zaman özeti.</summary>
    private sealed class VisitSummary
    {
        public DateTime First { get; set; }
        public DateTime Last { get; set; }
        public int Count { get; set; }
        public decimal Spend { get; set; }
    }

    public async Task<CustomerInsightsDto> Handle(GetCustomerInsightsQuery request, CancellationToken ct)
    {
        var turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var nowUtc   = DateTime.UtcNow;

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.StartDate, DateTimeKind.Unspecified), turkeyTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.EndDate.AddDays(1), DateTimeKind.Unspecified), turkeyTz);

        var tenantIds = request.TenantIds ?? new List<Guid> { request.TenantId };

        var completed = await _appointmentRepo.GetAllAsync(
            x => tenantIds.Contains(x.TenantId) &&
                 x.Status == AppointmentStatus.Completed, ct);

        var customers = await _customerRepo.GetAllAsync(x => tenantIds.Contains(x.TenantId), ct);
        var services  = await _serviceRepo.GetAllAsync(x => tenantIds.Contains(x.TenantId), ct);

        var rates = await _exchangeRates.GetRatesToTryAsync(
            services.Select(s => s.Currency).Distinct(), ct);

        // Gelir raporuyla aynı hesap: gerçekleşen tutar varsa o, yoksa hizmetin
        // liste fiyatının TRY karşılığı. İki rapor farklı sayı göstermemeli.
        decimal PriceInTry(AppointmentEntity apt)
        {
            if (apt.ActualTotalPrice.HasValue) return apt.ActualTotalPrice.Value;
            var svc = services.FirstOrDefault(s => s.Id == apt.ServiceId);
            if (svc is null) return 0;
            return (svc.Price ?? 0) * rates.GetValueOrDefault(svc.Currency.ToUpper(), 1m);
        }

        DateTime TurkeyDate(DateTime utc) =>
            TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), turkeyTz).Date;

        // ── Müşteri bazında tüm zaman özeti ─────────────────────────────────
        var summaries = new Dictionary<Guid, VisitSummary>();
        foreach (var apt in completed)
        {
            if (apt.CustomerId == Guid.Empty) continue;

            if (!summaries.TryGetValue(apt.CustomerId, out var s))
            {
                s = new VisitSummary { First = apt.StartTime, Last = apt.StartTime };
                summaries[apt.CustomerId] = s;
            }
            if (apt.StartTime < s.First) s.First = apt.StartTime;
            if (apt.StartTime > s.Last)  s.Last  = apt.StartTime;
            s.Count += 1;
            s.Spend += PriceInTry(apt);
        }

        // ── Seçili dönem ────────────────────────────────────────────────────
        var inRange = completed
            .Where(x => x.StartTime >= startUtc && x.StartTime < endUtc && x.CustomerId != Guid.Empty)
            .ToList();

        var periodRevenue = inRange.Sum(PriceInTry);
        var activeIds = inRange.Select(x => x.CustomerId).Distinct().ToList();

        var newCount = activeIds.Count(id =>
            summaries.TryGetValue(id, out var s) && s.First >= startUtc && s.First < endUtc);
        var returningCount = activeIds.Count - newCount;

        // Zaman serisi: her gün, o gün gelen farklı müşteriler "ilk kez" ve
        // "tekrar" diye ikiye ayrılıyor. Randevu değil müşteri sayılıyor —
        // aynı gün iki randevusu olan müşteri iki kişi gibi görünmesin.
        var timeline = inRange
            .GroupBy(x => TurkeyDate(x.StartTime))
            .OrderBy(g => g.Key)
            .Select(g =>
            {
                var point = new CustomerTimelinePointDto { Date = g.Key };
                foreach (var id in g.Select(x => x.CustomerId).Distinct())
                {
                    var firstVisitDay = summaries.TryGetValue(id, out var s)
                        ? TurkeyDate(s.First)
                        : g.Key;
                    if (firstVisitDay == g.Key) point.NewCount++;
                    else                        point.ReturningCount++;
                }
                return point;
            })
            .ToList();

        var customerById = customers.ToDictionary(c => c.Id);

        var topCustomers = inRange
            .GroupBy(x => x.CustomerId)
            .Select(g =>
            {
                customerById.TryGetValue(g.Key, out var c);
                return new CustomerRankDto
                {
                    Id         = g.Key,
                    FullName   = c?.FullName ?? "—",
                    Phone      = c?.Phone ?? "",
                    VisitCount = g.Count(),
                    TotalSpend = g.Sum(PriceInTry),
                    LastVisit  = g.Max(x => x.StartTime),
                };
            })
            // Silinmiş müşteri kaydı "—" olarak listede durmasın.
            .Where(x => customerById.ContainsKey(x.Id))
            .OrderByDescending(x => x.TotalSpend)
            .Take(10)
            .ToList();

        // ── Sadakat dağılımı (tüm zamanlar) ─────────────────────────────────
        // Kovalar sıralı: soldan sağa bağlılık artıyor. "Hiç gelmemiş" de
        // gösteriliyor — rehberde durup hiç uğramamış kayıtlar da bir bilgi.
        var buckets = new[]
        {
            new LoyaltyBucketDto { Label = "Hiç gelmemiş", Order = 0 },
            new LoyaltyBucketDto { Label = "1 ziyaret",    Order = 1 },
            new LoyaltyBucketDto { Label = "2 ziyaret",    Order = 2 },
            new LoyaltyBucketDto { Label = "3-5 ziyaret",  Order = 3 },
            new LoyaltyBucketDto { Label = "6-10 ziyaret", Order = 4 },
            new LoyaltyBucketDto { Label = "11+ ziyaret",  Order = 5 },
        };

        foreach (var customer in customers)
        {
            var count = summaries.TryGetValue(customer.Id, out var s) ? s.Count : 0;
            var index = count == 0 ? 0
                      : count == 1 ? 1
                      : count == 2 ? 2
                      : count <= 5 ? 3
                      : count <= 10 ? 4 : 5;
            buckets[index].Count++;
        }

        // ── Ziyaret aralığı ─────────────────────────────────────────────────
        var repeaters = summaries.Values.Where(s => s.Count >= 2).ToList();
        var averageGap = repeaters.Count > 0
            ? (int)Math.Round(repeaters.Average(s => (s.Last - s.First).TotalDays / (s.Count - 1)))
            : 0;

        // ── Kayıp riski ─────────────────────────────────────────────────────
        // En az iki kez gelmiş olanlar: bir kez gelip dönmeyen kişi "kayıp"
        // değil, hiç kazanılmamış müşteri — farklı bir iş.
        var threshold = nowUtc.AddDays(-request.AtRiskAfterDays);
        var atRisk = summaries
            .Where(kv => kv.Value.Count >= 2 && kv.Value.Last < threshold)
            .Select(kv =>
            {
                customerById.TryGetValue(kv.Key, out var c);
                return new AtRiskCustomerDto
                {
                    Id                  = kv.Key,
                    FullName            = c?.FullName ?? "—",
                    Phone               = c?.Phone ?? "",
                    VisitCount          = kv.Value.Count,
                    TotalSpend          = kv.Value.Spend,
                    LastVisit           = kv.Value.Last,
                    DaysSinceLastVisit  = (int)(nowUtc - kv.Value.Last).TotalDays,
                    HasMarketingConsent = c?.MarketingConsentAt != null,
                };
            })
            // Silinmiş ya da başka bir işletmeye ait müşteri kaydı listede durmasın.
            .Where(x => customerById.ContainsKey(x.Id))
            // En son gelen önce: dönme ihtimali en yüksek olan en üstte.
            .OrderByDescending(x => x.LastVisit)
            .ToList();

        return new CustomerInsightsDto
        {
            ActiveCustomers         = activeIds.Count,
            NewCustomers            = newCount,
            ReturningCustomers      = returningCount,
            RepeatRate              = activeIds.Count > 0
                                        ? Math.Round(returningCount * 100m / activeIds.Count, 1)
                                        : 0,
            AverageSpendPerCustomer = activeIds.Count > 0 ? periodRevenue / activeIds.Count : 0,
            PeriodRevenue           = periodRevenue,
            Timeline                = timeline,
            TopCustomers            = topCustomers,

            TotalCustomers       = customers.Count(),
            EverVisitedCustomers = summaries.Count(kv => customerById.ContainsKey(kv.Key)),
            AverageVisitGapDays  = averageGap,
            LoyaltyBuckets       = buckets.ToList(),
            AtRisk               = atRisk.Take(50).ToList(),
            AtRiskCount          = atRisk.Count,
            AtRiskWithConsent    = atRisk.Count(x => x.HasMarketingConsent),
            AtRiskAfterDays      = request.AtRiskAfterDays,
        };
    }
}
