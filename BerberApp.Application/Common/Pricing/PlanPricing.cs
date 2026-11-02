namespace BerberApp.Application.Common.Pricing;

/// <summary>Faturalama periyodu. Yıllıkta tutar peşin alınır.</summary>
public enum BillingPeriod
{
    Monthly,
    Annual,
}

/// <summary>
/// Abonelik fiyatları ve süreleri — tek kaynak.
///
/// Bu dosyadan önce aynı tablo dört ayrı yerde yazılıydı: iki ön yüz bileşeni
/// (fiyat sayfası ve ödeme sayfası), <c>IyzicoService</c> ve
/// <c>SubscriptionController</c>. İkisi Premium'u ₺2.999, ikisi ₺2.399
/// gösteriyordu — yani müşteriye bir tutar yazıp başka tutar tahsil edilecekti.
/// Müşteriye gösterilen değer esas alındı.
///
/// Tutar <b>her zaman sunucuda hesaplanır</b>. İstemciden gelen tutar bilgi
/// amaçlıdır; ödeme kaydına yazılmaz.
/// </summary>
public static class PlanPricing
{
    /// <summary>Plan adı (küçük harf) → aylık fiyat, TRY.</summary>
    public static readonly IReadOnlyDictionary<string, decimal> MonthlyPrices =
        new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            ["baslangic"]   = 899m,
            ["profesyonel"] = 1799m,
            ["premium"]     = 2999m,
        };

    /// <summary>Yıllık ödemede uygulanan indirim oranı.</summary>
    public const decimal AnnualDiscount = 0.20m;

    public static bool IsKnownPlan(string? plan) =>
        !string.IsNullOrWhiteSpace(plan) && MonthlyPrices.ContainsKey(plan);

    /// <summary>
    /// Tahsil edilecek tutar. Yıllıkta 12 ayın indirimli toplamı peşin alınır;
    /// ön yüzdeki "₺{indirimli aylık}/ay" gösterimi aynı hesabın sunumu.
    /// </summary>
    public static decimal PriceFor(string plan, BillingPeriod period)
    {
        if (!MonthlyPrices.TryGetValue(plan, out var monthly)) return 0m;

        return period == BillingPeriod.Annual
            ? Math.Round(monthly * 12 * (1 - AnnualDiscount), MidpointRounding.AwayFromZero)
            : monthly;
    }

    /// <summary>Ödeme sonrası aboneliğin biteceği an.</summary>
    public static DateTime ExpiryFrom(DateTime start, BillingPeriod period) =>
        period == BillingPeriod.Annual ? start.AddYears(1) : start.AddMonths(1);

    /// <summary>Havale akışındaki <c>DurationDays</c> karşılığı.</summary>
    public static int DurationDays(BillingPeriod period) =>
        period == BillingPeriod.Annual ? 365 : 30;

    public static BillingPeriod FromDurationDays(int days) =>
        days >= 365 ? BillingPeriod.Annual : BillingPeriod.Monthly;

    /// <summary>"annual" / "yearly" / "yillik" → yıllık; gerisi aylık.</summary>
    public static BillingPeriod Parse(string? value) =>
        value?.Trim().ToLowerInvariant() switch
        {
            "annual" or "yearly" or "yillik" or "yıllık" => BillingPeriod.Annual,
            _                                            => BillingPeriod.Monthly,
        };

    /// <summary>Sepet kaleminde ve sözleşme metninde görünen süre.</summary>
    public static string PeriodLabel(BillingPeriod period) =>
        period == BillingPeriod.Annual ? "1 Yıl" : "1 Ay";
}
