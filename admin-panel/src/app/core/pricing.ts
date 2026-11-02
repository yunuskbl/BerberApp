/**
 * Plan fiyatları — ön yüzdeki tek kaynak.
 *
 * Aynı tablo eskiden birçok bileşende ayrı ayrı yazılıydı ve kopyalar
 * birbirinden ayrışmıştı. Artık iki yerde duruyor: burası (gösterim) ve
 * `BerberApp.Application/Common/Pricing/PlanPricing.cs` (tahsilat).
 * Fiyat değişikliğinde İKİSİ birlikte güncellenmeli.
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
  baslangic:   { key: 'baslangic',   label: 'Başlangıç',   icon: '🌱', monthly: 699  },
  profesyonel: { key: 'profesyonel', label: 'Profesyonel', icon: '⚡', monthly: 1499 },
  premium:     { key: 'premium',     label: 'Premium',     icon: '👑', monthly: 2399 },
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

/**
 * Tutarı yerel biçimde yazar: tr-TR'de ₺1.499. Angular'ın `number` pipe'ı
 * varsayılan en-US yerelini kullanıp "1,499" basıyordu; ham bağlama da
 * ayırıcısız "1499" basıyordu. Fiyat gösteren her ekran bunu kullanmalı.
 */
export function formatPrice(value: number, locale = 'tr-TR'): string {
  return new Intl.NumberFormat(locale, { maximumFractionDigits: 0 }).format(value);
}

export function durationDays(period: BillingPeriod): number {
  return period === 'annual' ? 365 : 30;
}
