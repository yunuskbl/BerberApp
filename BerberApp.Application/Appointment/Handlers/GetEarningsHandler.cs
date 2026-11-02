using BerberApp.Application.Appointment.Queries;
using BerberApp.Application.Common.Interfaces;
using BerberApp.Domain.Enums;
using MediatR;

namespace BerberApp.Application.Appointment.Handlers;

public class GetEarningsHandler : IRequestHandler<GetEarningsQuery, EarningsDto>
{
    private readonly IGenericRepository<AppointmentEntity> _appointmentRepo;
    private readonly IGenericRepository<ServiceEntity> _serviceRepo;
    private readonly IGenericRepository<StaffEntity> _staffRepo;
    private readonly IGenericRepository<CustomerEntity> _customerRepo;
    private readonly IGenericRepository<ExpenseEntity> _expenseRepo;
    private readonly IGenericRepository<PriceDifferenceEntity> _priceDiffRepo;
    private readonly IExchangeRateService _exchangeRates;

    public GetEarningsHandler(
        IGenericRepository<AppointmentEntity> appointmentRepo,
        IGenericRepository<ServiceEntity> serviceRepo,
        IGenericRepository<StaffEntity> staffRepo,
        IGenericRepository<CustomerEntity> customerRepo,
        IGenericRepository<ExpenseEntity> expenseRepo,
        IGenericRepository<PriceDifferenceEntity> priceDiffRepo,
        IExchangeRateService exchangeRates)
    {
        _appointmentRepo = appointmentRepo;
        _serviceRepo = serviceRepo;
        _staffRepo = staffRepo;
        _customerRepo = customerRepo;
        _expenseRepo = expenseRepo;
        _priceDiffRepo = priceDiffRepo;
        _exchangeRates = exchangeRates;
    }

    public async Task<EarningsDto> Handle(GetEarningsQuery request, CancellationToken ct)
    {
        var turkeyTz = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");
        var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, turkeyTz);
        var todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0);
        var todayEnd = todayStart.AddDays(1);
        var weekStart = todayStart.AddDays(-(int)now.DayOfWeek);
        var monthStart = new DateTime(now.Year, now.Month, 1);

        var startUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.StartDate, DateTimeKind.Unspecified), turkeyTz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(request.EndDate.AddDays(1), DateTimeKind.Unspecified), turkeyTz);

        // Önceki dönem: aynı uzunlukta, hemen öncesi. 1-30 Ekim seçiliyse
        // 1-30 Eylül ile karşılaştırılır.
        var periodDays        = Math.Max(1, (int)(request.EndDate.Date - request.StartDate.Date).TotalDays + 1);
        var previousStartDate = request.StartDate.Date.AddDays(-periodDays);
        var previousEndDate   = request.StartDate.Date.AddDays(-1);
        var previousStartUtc  = TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(previousStartDate, DateTimeKind.Unspecified), turkeyTz);

        var query = new List<AppointmentEntity>();
        var tenantIds = request.TenantIds ?? new List<Guid> { request.TenantId };

        // Durum kırılımı iptal ve gelmedi sayılarını da istiyor, bu yüzden
        // randevular duruma göre süzülmeden çekiliyor; "tamamlandı" ayrımı
        // aşağıda bellekte yapılıyor. Tek sorgu, aynı tablo taraması.
        var everyAppointment = await _appointmentRepo.GetAllAsync(
            x => tenantIds.Contains(x.TenantId), ct);

        if (request.StaffId.HasValue)
            everyAppointment = everyAppointment.Where(x => x.StaffId == request.StaffId.Value).ToList();

        var allAppointments = everyAppointment
            .Where(x => x.Status == AppointmentStatus.Completed)
            .ToList();

        var appointments = allAppointments
            .Where(x => x.StartTime >= startUtc && x.StartTime < endUtc)
            .ToList();

        // Dönemdeki bütün randevular (tamamlanmayanlar dahil) — durum kırılımı için.
        var appointmentsInRangeAnyStatus = everyAppointment
            .Where(x => x.StartTime >= startUtc && x.StartTime < endUtc)
            .ToList();

        var previousAppointments = allAppointments
            .Where(x => x.StartTime >= previousStartUtc && x.StartTime < startUtc)
            .ToList();

        var today = appointments
            .Where(x => x.StartTime >= TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(todayStart, DateTimeKind.Unspecified), turkeyTz) &&
                x.StartTime < TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(todayEnd, DateTimeKind.Unspecified), turkeyTz))
            .ToList();

        var thisWeek = appointments
            .Where(x => x.StartTime >= TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(weekStart, DateTimeKind.Unspecified), turkeyTz) &&
                x.StartTime < TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(weekStart.AddDays(7), DateTimeKind.Unspecified), turkeyTz))
            .ToList();

        var thisMonth = appointments
            .Where(x => x.StartTime >= TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(monthStart, DateTimeKind.Unspecified), turkeyTz) &&
                x.StartTime < TimeZoneInfo.ConvertTimeToUtc(
                DateTime.SpecifyKind(monthStart.AddMonths(1), DateTimeKind.Unspecified), turkeyTz))
            .ToList();

        var services  = await _serviceRepo.GetAllAsync(x => tenantIds.Contains(x.TenantId), ct);
        var staff     = await _staffRepo.GetAllAsync(x => tenantIds.Contains(x.TenantId), ct);
        var customers = await _customerRepo.GetAllAsync(x => tenantIds.Contains(x.TenantId), ct);

        // Döviz kurları
        var currencies = services.Select(s => s.Currency).Distinct();
        var rates = await _exchangeRates.GetRatesToTryAsync(currencies, ct);
        var rateDate = DateTime.UtcNow.ToString("yyyy-MM-dd");

        decimal ServicePrice(Guid? serviceId) =>
            services.FirstOrDefault(s => s.Id == serviceId)?.Price ?? 0;
        decimal ServicePriceInTry(Guid? serviceId)
        {
            var svc = services.FirstOrDefault(s => s.Id == serviceId);
            if (svc is null) return 0;
            return (svc.Price ?? 0) * (rates.GetValueOrDefault(svc.Currency.ToUpper(), 1m));
        }

        // ActualTotalPrice varsa onu kullan (her zaman TRY cinsinden), yoksa servis fiyatına dön
        decimal EffectivePriceInTry(AppointmentEntity apt) =>
            apt.ActualTotalPrice.HasValue
                ? apt.ActualTotalPrice.Value
                : ServicePriceInTry(apt.ServiceId);

        var totalEarnings  = appointments.Sum(x => x.ActualTotalPrice ?? ServicePrice(x.ServiceId));
        var todayEarnings  = today.Sum(EffectivePriceInTry);
        var weekEarnings   = thisWeek.Sum(EffectivePriceInTry);
        var monthEarnings  = thisMonth.Sum(EffectivePriceInTry);
        var totalInTry     = appointments.Sum(EffectivePriceInTry);

        // Para birimine göre gruplama:
        // ActualTotalPrice olan randevular TRY grubunda sayılır, olmayanlar kendi para biriminde
        var byCurrency = appointments
            .GroupBy(x => x.ActualTotalPrice.HasValue
                ? "TRY"
                : services.FirstOrDefault(s => s.Id == x.ServiceId)?.Currency ?? "TRY")
            .Select(g =>
            {
                var currency = g.Key.ToUpper();
                var total = g.Sum(x => x.ActualTotalPrice ?? ServicePrice(x.ServiceId));
                var rate = rates.GetValueOrDefault(currency, 1m);
                return new CurrencyEarningDto
                {
                    Currency = currency,
                    TotalEarnings = total,
                    TotalInTry = total * rate,
                    ExchangeRate = rate,
                    AppointmentCount = g.Count()
                };
            })
            .OrderByDescending(x => x.TotalInTry)
            .ToList();

        // Gün kırılımı Türkiye saatine göre yapılır. UTC gününe göre gruplanırsa
        // 00:00–03:00 arası randevular bir önceki güne düşer ve grafik, aynı
        // yanıttaki Bugün/Bu Hafta/Bu Ay değerleriyle çelişir.
        var daily = appointments
            .GroupBy(x => TimeZoneInfo.ConvertTimeFromUtc(
                DateTime.SpecifyKind(x.StartTime, DateTimeKind.Utc), turkeyTz).Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyEarningDto
            {
                Date = g.Key,
                Earnings = g.Sum(EffectivePriceInTry),
                AppointmentCount = g.Count()
            })
            .ToList();

        var byStaff = appointments
            .GroupBy(x => x.StaffId)
            .Select(g => new StaffEarningDto
            {
                StaffId = g.Key.ToString(),
                StaffName = staff.FirstOrDefault(s => s.Id == g.Key)?.FullName ?? "Unknown",
                TotalEarnings = g.Sum(EffectivePriceInTry),
                AppointmentCount = g.Count(),
                Average = g.Count() > 0 ? g.Sum(EffectivePriceInTry) / g.Count() : 0
            })
            .ToList();

        var byService = appointments
            .GroupBy(x => x.ServiceId)
            .Select(g => new ServiceEarningDto
            {
                ServiceId = g.Key.ToString(),
                ServiceName = services.FirstOrDefault(s => s.Id == g.Key)?.Name ?? "Unknown",
                Currency = services.FirstOrDefault(s => s.Id == g.Key)?.Currency ?? "TRY",
                Price = services.FirstOrDefault(s => s.Id == g.Key)?.Price ?? 0,
                TotalEarnings = g.Sum(EffectivePriceInTry),
                AppointmentCount = g.Count()
            })
            .ToList();

        // ── Fiyat farkı istatistikleri ─────────────────────────────────────
        var priceDiffs = await _priceDiffRepo.GetAllAsync(
            x => tenantIds.Contains(x.TenantId) &&
                 x.CompletedAt >= startUtc && x.CompletedAt < endUtc, ct);
        var totalPriceDiff = priceDiffs.Sum(x => x.Difference);
        var priceDiffCount = priceDiffs.Count;

        var appointmentMap = appointments.ToDictionary(x => x.Id);
        var priceDiffDetails = priceDiffs
            .OrderByDescending(x => x.CompletedAt)
            .Select(pd =>
            {
                appointmentMap.TryGetValue(pd.AppointmentId, out var apt);
                return new PriceDifferenceDetailDto
                {
                    AppointmentId = pd.AppointmentId,
                    CustomerName  = customers.FirstOrDefault(c => c.Id == apt?.CustomerId)?.FullName ?? "—",
                    StaffName     = staff.FirstOrDefault(s => s.Id == apt?.StaffId)?.FullName ?? "—",
                    ServiceName   = services.FirstOrDefault(s => s.Id == apt?.ServiceId)?.Name ?? "—",
                    OriginalPrice = pd.OriginalPrice,
                    ActualPrice   = pd.ActualPrice,
                    Difference    = pd.Difference,
                    CompletedAt   = pd.CompletedAt,
                };
            })
            .ToList();

        // ── Giderler (Expenses) ─────────────────────────────────────────────
        // Aralık önceki dönemi de kapsayacak şekilde geniş çekiliyor; seçili
        // dönem ile karşılaştırma dönemi bellekte ayrılıyor.
        var expensesWithPrevious = await _expenseRepo.GetAllAsync(
            x => tenantIds.Contains(x.TenantId) &&
                 x.Date >= previousStartUtc && x.Date < endUtc, ct);

        var allExpenses = expensesWithPrevious
            .Where(x => x.Date >= startUtc)
            .ToList();

        // Tüm gider para birimlerini döviz kurlarına ekle
        var expenseCurrencies = expensesWithPrevious.Select(e => e.Currency).Distinct();
        foreach (var cur in expenseCurrencies)
        {
            if (!rates.ContainsKey(cur.ToUpper()))
            {
                var extraRates = await _exchangeRates.GetRatesToTryAsync([cur], ct);
                foreach (var kv in extraRates) rates[kv.Key] = kv.Value;
            }
        }

        var totalExpenses = allExpenses.Sum(e =>
            e.Amount * rates.GetValueOrDefault(e.Currency.ToUpper(), 1m));

        var expenseByCategory = allExpenses
            .GroupBy(e => string.IsNullOrWhiteSpace(e.Category) ? "Diğer" : e.Category)
            .Select(g => new ExpenseSummaryDto
            {
                Category = g.Key,
                Total = g.Sum(e => e.Amount * rates.GetValueOrDefault(e.Currency.ToUpper(), 1m)),
                Count = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .ToList();

        // Güne göre gider — net kâr grafiği için. Gider tarihleri gün bazında
        // (saat bileşeni olmadan) tutuluyor, bu yüzden gelir tarafındaki gibi
        // saat dilimi çevrimi yapılmaz; aksi hâlde kayıtlar bir gün geri kayar.
        var expenseDaily = allExpenses
            .GroupBy(e => e.Date.Date)
            .OrderBy(g => g.Key)
            .Select(g => new DailyExpenseDto
            {
                Date  = g.Key,
                Total = g.Sum(e => e.Amount * rates.GetValueOrDefault(e.Currency.ToUpper(), 1m)),
                Count = g.Count()
            })
            .ToList();

        // ── Önceki dönem karşılaştırması ────────────────────────────────────
        var previousTotalInTry = previousAppointments.Sum(EffectivePriceInTry);
        var previousExpenses = expensesWithPrevious
            .Where(e => e.Date >= previousStartUtc && e.Date < startUtc)
            .Sum(e => e.Amount * rates.GetValueOrDefault(e.Currency.ToUpper(), 1m));

        // ── Randevu durumları ───────────────────────────────────────────────
        // Sıra bilinçli: tamamlanan önce, sorunlu durumlar sonra. Enum adı
        // olduğu gibi gönderiliyor; Türkçe etiket istemcide, çeviri orada.
        var statusOrder = new[]
        {
            AppointmentStatus.Completed,
            AppointmentStatus.Confirmed,
            AppointmentStatus.Pending,
            AppointmentStatus.Cancelled,
            AppointmentStatus.NoShow,
        };

        var statusBreakdown = statusOrder
            .Select((status, index) => new StatusBreakdownDto
            {
                Status = status.ToString(),
                Count  = appointmentsInRangeAnyStatus.Count(x => x.Status == status),
                Order  = index,
            })
            .ToList();

        // ── Yoğunluk: gün × saat ────────────────────────────────────────────
        // Saat Türkiye saatine çevriliyor; UTC saatiyle gruplamak bütün
        // haritayı üç saat kaydırırdı.
        var peakCells = appointments
            .Select(x => new
            {
                Local = TimeZoneInfo.ConvertTimeFromUtc(
                    DateTime.SpecifyKind(x.StartTime, DateTimeKind.Utc), turkeyTz),
                Appointment = x,
            })
            .GroupBy(x => new { Day = (int)x.Local.DayOfWeek, x.Local.Hour })
            .Select(g => new PeakHourCellDto
            {
                DayOfWeek = g.Key.Day,
                Hour      = g.Key.Hour,
                Count     = g.Count(),
                Revenue   = g.Sum(x => EffectivePriceInTry(x.Appointment)),
            })
            .OrderBy(x => x.DayOfWeek).ThenBy(x => x.Hour)
            .ToList();

        // En yoğun dilimler ve o dilimde ne yapıldığı: "cumartesi 14:00'te
        // kalabalık" bilgisi tek başına eksik, hangi hizmetin yığıldığı
        // personel planlamasını değiştiriyor.
        var peakSlots = peakCells
            .OrderByDescending(x => x.Count)
            .ThenByDescending(x => x.Revenue)
            .Take(5)
            .Select(cell =>
            {
                var slotAppointments = appointments.Where(x =>
                {
                    var local = TimeZoneInfo.ConvertTimeFromUtc(
                        DateTime.SpecifyKind(x.StartTime, DateTimeKind.Utc), turkeyTz);
                    return (int)local.DayOfWeek == cell.DayOfWeek && local.Hour == cell.Hour;
                });

                return new PeakSlotDto
                {
                    DayOfWeek = cell.DayOfWeek,
                    Hour      = cell.Hour,
                    Count     = cell.Count,
                    Revenue   = cell.Revenue,
                    Services  = slotAppointments
                        .GroupBy(x => services.FirstOrDefault(s => s.Id == x.ServiceId)?.Name ?? "—")
                        .Select(g => new SlotServiceDto { ServiceName = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(3)
                        .ToList(),
                };
            })
            .ToList();

        return new EarningsDto
        {
            TotalEarnings = totalEarnings,
            TotalInTry = totalInTry,
            ExchangeRateDate = rateDate,
            TotalAppointments = appointments.Count,
            AveragePerAppointment = appointments.Count > 0 ? totalInTry / appointments.Count : 0,
            TodayEarnings = todayEarnings,
            TodayAppointments = today.Count,
            WeekEarnings = weekEarnings,
            WeekAppointments = thisWeek.Count,
            MonthEarnings = monthEarnings,
            MonthAppointments = thisMonth.Count,
            ByCurrency = byCurrency,
            Daily = daily,
            ByStaff = byStaff,
            ByService = byService,
            TotalExpenses = totalExpenses,
            NetProfit = totalInTry - totalExpenses,
            ExpenseByCategory = expenseByCategory,
            ExpenseDaily = expenseDaily,
            TotalPriceDifference = totalPriceDiff,
            PriceDifferenceCount = priceDiffCount,
            PriceDifferences = priceDiffDetails,

            PreviousStartDate        = previousStartDate,
            PreviousEndDate          = previousEndDate,
            PreviousTotalInTry       = previousTotalInTry,
            PreviousTotalAppointments = previousAppointments.Count,
            PreviousTotalExpenses    = previousExpenses,
            PreviousNetProfit        = previousTotalInTry - previousExpenses,

            StatusBreakdown       = statusBreakdown,
            AllAppointments       = appointmentsInRangeAnyStatus.Count,
            CancelledAppointments = appointmentsInRangeAnyStatus.Count(x => x.Status == AppointmentStatus.Cancelled),
            NoShowAppointments    = appointmentsInRangeAnyStatus.Count(x => x.Status == AppointmentStatus.NoShow),

            PeakHours = peakCells,
            PeakSlots = peakSlots,
        };
    }
}