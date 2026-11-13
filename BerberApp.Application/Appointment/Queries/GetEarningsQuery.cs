using MediatR;

namespace BerberApp.Application.Appointment.Queries;

public class GetEarningsQuery : IRequest<EarningsDto>
{
    public Guid TenantId { get; set; }
    public List<Guid>? TenantIds { get; set; }  // null → single TenantId; set → multi-tenant (consolidated/branch)
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public Guid? StaffId { get; set; }
}

public class EarningsDto
{
    public decimal TotalEarnings { get; set; }         // Ana para birimi toplamı
    public decimal TotalInTry { get; set; }            // Tüm dövizler TRY'ye çevrilmiş toplam
    public string ExchangeRateDate { get; set; } = ""; // Kur tarihi
    public int TotalAppointments { get; set; }
    public decimal AveragePerAppointment { get; set; }

    public decimal TodayEarnings { get; set; }
    public int TodayAppointments { get; set; }

    public decimal WeekEarnings { get; set; }
    public int WeekAppointments { get; set; }

    public decimal MonthEarnings { get; set; }
    public int MonthAppointments { get; set; }

    // ── Gelir kaynakları ────────────────────────────────────────────────────
    // TotalInTry = ServiceRevenueInTry + PackageRevenueInTry. Personel, hizmet
    // ve yoğunluk kırılımları randevulardan; paket tahsilatı yalnızca toplamlarda.
    public decimal ServiceRevenueInTry { get; set; }
    /// <summary>Dönemde tahsil edilen (iptal edilmemiş) paket ödemeleri, tahsilat gününe göre.</summary>
    public decimal PackageRevenueInTry { get; set; }
    public int PackagePaymentCount { get; set; }
    public List<PackageSalesDto> PackageSales { get; set; } = new();
    /// <summary>Bugün itibarıyla iptal edilmemiş paketlerden alınacak kalan borç (döneme bağlı değil).</summary>
    public decimal PackageOutstandingInTry { get; set; }
    public int PackageOutstandingCount { get; set; }
    public decimal PreviousServiceRevenueInTry { get; set; }
    public decimal PreviousPackageRevenueInTry { get; set; }

    public List<CurrencyEarningDto> ByCurrency { get; set; } = new();
    public List<DailyEarningDto> Daily { get; set; } = new();
    public List<StaffEarningDto> ByStaff { get; set; } = new();
    public List<ServiceEarningDto> ByService { get; set; } = new();

    // P&L
    public decimal TotalExpenses { get; set; }       // Seçili dönem toplam gider (TRY)
    public decimal NetProfit { get; set; }            // TotalInTry - TotalExpenses
    public List<ExpenseSummaryDto> ExpenseByCategory { get; set; } = new();
    /// <summary>
    /// Güne göre gider (TRY'ye çevrilmiş). Gider listesi tutarları kendi para
    /// biriminde döndüğü için net kâr grafiği onlardan hesaplanamaz; çevrim
    /// kurları yalnızca burada, sunucuda mevcut.
    /// </summary>
    public List<DailyExpenseDto> ExpenseDaily { get; set; } = new();

    // Fiyat farkı istatistikleri
    public decimal TotalPriceDifference { get; set; }
    public int PriceDifferenceCount { get; set; }
    public List<PriceDifferenceDetailDto> PriceDifferences { get; set; } = new();

    // ── Önceki dönem (aynı uzunlukta, hemen öncesi) ────────────────────────
    // Karşılaştırma sunucuda hesaplanıyor: handler zaten işletmenin bütün
    // randevularını belleğe alıyor, önceki dönemi süzmek fazladan sorgu
    // getirmiyor. İstemciden ikinci bir istek atmak gereksiz olurdu.
    public DateTime PreviousStartDate { get; set; }
    public DateTime PreviousEndDate { get; set; }
    public decimal PreviousTotalInTry { get; set; }
    public int PreviousTotalAppointments { get; set; }
    public decimal PreviousTotalExpenses { get; set; }
    public decimal PreviousNetProfit { get; set; }

    // ── Randevu durumları ───────────────────────────────────────────────────
    /// <summary>Dönemdeki randevuların durum kırılımı (iptal, gelmedi dahil).</summary>
    public List<StatusBreakdownDto> StatusBreakdown { get; set; } = new();
    /// <summary>Dönemdeki toplam randevu — tamamlanmayanlar dahil.</summary>
    public int AllAppointments { get; set; }
    public int CancelledAppointments { get; set; }
    public int NoShowAppointments { get; set; }

    // ── Yoğunluk ────────────────────────────────────────────────────────────
    /// <summary>Gün × saat kırılımı; yalnızca randevu olan hücreler.</summary>
    public List<PeakHourCellDto> PeakHours { get; set; } = new();
    /// <summary>En yoğun saat dilimleri ve o dilimde uygulanan hizmetler.</summary>
    public List<PeakSlotDto> PeakSlots { get; set; } = new();
}

public class StatusBreakdownDto
{
    /// <summary>Enum adı (Completed, Cancelled, NoShow, …) — etiket istemcide.</summary>
    public string Status { get; set; } = string.Empty;
    public int Count { get; set; }
    public int Order { get; set; }
}

public class PeakHourCellDto
{
    /// <summary>0 = pazar … 6 = cumartesi (DayOfWeek ile aynı).</summary>
    public int DayOfWeek { get; set; }
    /// <summary>Türkiye saatiyle randevunun başladığı saat (0-23).</summary>
    public int Hour { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
}

public class PeakSlotDto
{
    public int DayOfWeek { get; set; }
    public int Hour { get; set; }
    public int Count { get; set; }
    public decimal Revenue { get; set; }
    /// <summary>O saat diliminde en çok uygulanan hizmetler.</summary>
    public List<SlotServiceDto> Services { get; set; } = new();
}

public class SlotServiceDto
{
    public string ServiceName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class PriceDifferenceDetailDto
{
    public Guid AppointmentId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string StaffName { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public decimal OriginalPrice { get; set; }
    public decimal ActualPrice { get; set; }
    public decimal Difference { get; set; }
    public DateTime CompletedAt { get; set; }
}

public class ExpenseSummaryDto
{
    public string Category { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int Count { get; set; }
}

public class CurrencyEarningDto
{
    public string Currency { get; set; } = string.Empty;
    public decimal TotalEarnings { get; set; }
    public decimal TotalInTry { get; set; }
    public decimal ExchangeRate { get; set; }  // 1 X = ? TRY
    public int AppointmentCount { get; set; }
}

public class PackageSalesDto
{
    public string Name { get; set; } = string.Empty;
    /// <summary>Dönemde ödeme alınan farklı paket (müşteri) sayısı.</summary>
    public int PackageCount { get; set; }
    public int PaymentCount { get; set; }
    public decimal Total { get; set; }
}

public class DailyEarningDto
{
    public DateTime Date { get; set; }
    /// <summary>Hizmet + paket tahsilatı.</summary>
    public decimal Earnings { get; set; }
    /// <summary>Bunun paket tahsilatı kısmı.</summary>
    public decimal PackageEarnings { get; set; }
    public int AppointmentCount { get; set; }
}

public class DailyExpenseDto
{
    public DateTime Date { get; set; }
    public decimal Total { get; set; }   // TRY
    public int Count { get; set; }
}

public class StaffEarningDto
{
    public string? StaffId { get; set; }
    public string? StaffName { get; set; }
    public decimal TotalEarnings { get; set; }
    public int AppointmentCount { get; set; }
    public decimal Average { get; set; }
}

public class ServiceEarningDto
{
    public string? ServiceId { get; set; }
    public string? ServiceName { get; set; }
    public string Currency { get; set; } = "TRY";
    public decimal TotalEarnings { get; set; }
    public int AppointmentCount { get; set; }
    public decimal Price { get; set; }
}