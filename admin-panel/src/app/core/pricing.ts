/**
 * Plan fiyatları — ön yüzdeki tek kaynak.
 *
 * Aynı tablo fiyat sayfasında ve ödeme sayfasında ayrı ayrı yazılıydı; sunucu
 * tarafında da iki kopyası vardı ve Premium orada ₺2.399 görünüyordu. Müşteriye
 * gösterilen ₺2.999 esas alındı ve dört kopya ikiye indi: burası (gösterim) ve
 * `BerberApp.Application/Common/Pricing/PlanPricing.cs` (tahsilat).
 *
 * Tahsil edilen tutarı her zaman sunucu hesaplar; buradaki değerler yalnızca
 * gösterim içindir. İkisi ayrışırsa sunucu kazanır ve uyuşmazlık loglanır.
 */

export type BillingPeriod = 'monthly' | 'annual';

export interface PlanInfo {
  key: string;
  label: string;
  icon: string;
  /** Aylık liste fiyatı, TRY. */
  monthly: number;
}

/** Yıllık ödemede uygulanan indirim. */
export const ANNUAL_DISCOUNT = 0.20;

export const PLANS: Record<string, PlanInfo> = {
  baslangic:   { key: 'baslangic',   label: 'Başlangıç',   icon: '🌱', monthly: 899  },
  profesyonel: { key: 'profesyonel', label: 'Profesyonel', icon: '⚡', monthly: 1799 },
  premium:     { key: 'premium',     label: 'Premium',     icon: '👑', monthly: 2999 },
};

/** Periyoda göre tahsil edilecek toplam tutar. */
export function priceFor(monthly: number, period: BillingPeriod): number {
  return period === 'annual'
    ? Math.round(monthly * 12 * (1 - ANNUAL_DISCOUNT))
    : monthly;
}

/** Yıllıkta kartlarda gösterilen indirimli aylık karşılık. */
export function displayMonthly(monthly: number, period: BillingPeriod): number {
  return period === 'annual' ? Math.round(monthly * (1 - ANNUAL_DISCOUNT)) : monthly;
}

/** Yıllık ödemede bir yılda kalan tutar. */
export function annualSaving(monthly: number): number {
  return Math.round(monthly * 12 * ANNUAL_DISCOUNT);
}

export function durationDays(period: BillingPeriod): number {
  return period === 'annual' ? 365 : 30;
}
