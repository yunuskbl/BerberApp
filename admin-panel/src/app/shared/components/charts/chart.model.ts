/**
 * Grafik ortak modeli, renk jetonları ve eksen yardımcıları.
 *
 * Paletin kuralı: her renk tek bir iş yapar. `series` kimlik/büyüklük (tek seri),
 * `positive`/`negative` ise işaret (sıfır çizgisinin hangi tarafı) taşır.
 * Mavi↔kırmızı çifti bilinçli: sıcak/soğuk kutuplar renk körlüğünde de ayrışıyor
 * (OKLab ΔE 29.9 protanopi, 38.2 normal görme — beyaz zemine karşı doğrulandı).
 * Yeşil↔kırmızı çifti muhasebe sezgisine uyardı ama en yaygın renk körlüğü
 * türünde birbirine karışıyor; bu yüzden kâr mavi.
 *
 * Renkleri değiştirirken doğrulayıcıyı beyaz zemine karşı yeniden çalıştır —
 * göz kararıyla "yeterince farklı" denmez.
 */

/** Zaman serisi / kategori noktası. */
export interface ChartPoint {
  /** Eksende görünen kısa etiket (örn. "5 Oca"). */
  label: string;
  /** Tooltip ve tabloda görünen tam etiket (örn. "5 – 11 Ocak 2026"). */
  fullLabel: string;
  /** Çizilen değer. */
  value: number;
  /** Tooltip/tabloda değerin yanında gösterilecek ek bilgi (örn. "12 randevu"). */
  note?: string;
}

/** Isı haritası hücresi. */
export interface HeatCell {
  /** Satır indeksi (0 tabanlı). */
  row: number;
  /** Sütun indeksi (0 tabanlı). */
  col: number;
  value: number;
  /** Tooltip ve tablo için okunur açıklama. */
  label: string;
  /** Tooltip'te değerin altında görünen ek bilgi. */
  note?: string;
}

/**
 * Sıralı mor rampa — ısı haritası gibi sürekli büyüklükler için.
 * Tek hue, açıktan koyuya; doğrulandı: monoton açıklık, komşu adım farkı
 * ≥ 0.06, hue yayılımı 1°. En açık adım zemine yakın duruyor; bu sequential
 * ölçekte kasıtlı — "sıfıra yakın" zaten geri çekilmeli. Hiç değeri olmayan
 * hücre boyanmıyor, böylece "yok" ile "az" karışmıyor.
 */
export const HEAT_RAMP = ['#ddd6fe', '#c4b5fd', '#a78bfa', '#8b5cf6', '#6d28d9'] as const;

export const CHART_INK = {
  /** Tek serinin rengi — marka moru. */
  series:     '#7c3aed',
  /** Alan dolgusu: serinin %10 opaklıkta yıkaması, doygun blok değil. */
  seriesWash: 'rgba(124, 58, 237, 0.10)',
  /**
   * İkinci seri. Marka moruyla yan yana duracağı için turuncu: mor↔mavi
   * çifti en yaygın renk körlüğünde tek renge düşüyor (OKLab ΔE 0.4),
   * mor↔turuncu arası 32.
   */
  accent:     '#eb6834',
  /** Sıfır çizgisinin üstü (kâr). */
  positive:   '#2563eb',
  /** Sıfır çizgisinin altı (zarar). */
  negative:   '#dc2626',
  /** Izgara: kıl çizgi, zeminden bir ton koyu, kesiksiz. */
  grid:       '#f0f1f3',
  /** Taban/eksen çizgisi. */
  axis:       '#d1d5db',
  /** Eksen etiketleri. */
  muted:      '#9ca3af',
  /** İkincil metin. */
  secondary:  '#6b7280',
  /** Ana metin. */
  primary:    '#111827',
  /** İşaretlerin üst üste bindiği yerde okunurluğu koruyan halka. */
  surface:    '#ffffff',
} as const;

/**
 * Eksen için "yuvarlak" kademe değerleri üretir: 0, 500, 1.000, 1.500 gibi.
 * Dönen son değer her zaman `max`'a eşit ya da ondan büyüktür, böylece en uzun
 * işaret ızgaranın dışına taşmaz.
 */
export function niceTicks(max: number, count = 4, integerOnly = false): number[] {
  if (!isFinite(max) || max <= 0) return [0, 1];

  const step  = niceStep(max / count, integerOnly);
  const steps = Math.max(1, Math.ceil(max / step - 1e-9));
  // Kademeler çarpımla üretiliyor; `v += step` ile biriktirmek ondalıklı
  // adımlarda 0.30000000000000004 gibi değerler doğuruyor.
  return Array.from({ length: steps + 1 }, (_, i) => roundStep(i * step, step));
}

/**
 * Sıfırın iki yanına da uzanan eksen için kademe değerleri. Kollar eşit adımlı
 * ama eşit uzunlukta değil: veri -6.400 ile +2.300 arasındaysa ekseni ±10.000'e
 * açmak grafiğin yarısını boşa harcar.
 */
export function divergingTicks(min: number, max: number, count = 4): number[] {
  const low  = Math.min(0, min);
  const high = Math.max(0, max);
  const span = high - low;
  if (span <= 0) return [-1, 0, 1];

  const step  = niceStep(span / count);
  const from  = Math.floor(low / step + 1e-9);
  const to    = Math.ceil(high / step - 1e-9);

  return Array.from({ length: to - from + 1 }, (_, i) => roundStep((from + i) * step, step));
}

/**
 * Ham adımı 1 / 2 / 2,5 / 5 / 10 ailesine yuvarlar — okunur kademeler.
 * `integerOnly`: randevu sayısı gibi tam sayı eksenlerde 2,5'lik kademe
 * ("2,5 randevu") anlamsız olduğu için o aile atlanır.
 */
function niceStep(raw: number, integerOnly = false): number {
  if (!isFinite(raw) || raw <= 0) return 1;
  const magnitude  = Math.pow(10, Math.floor(Math.log10(raw)));
  const normalized = raw / magnitude;
  const nice =
    normalized <= 1                  ? 1 :
    normalized <= 2                  ? 2 :
    normalized <= 2.5 && !integerOnly ? 2.5 :
    normalized <= 5                  ? 5 : 10;
  const step = nice * magnitude;
  return integerOnly ? Math.max(1, Math.round(step)) : step;
}

/** Kayan nokta artıklarını temizler: 0.30000000000000004 → 0.3 */
function roundStep(value: number, step: number): number {
  const decimals = Math.max(0, Math.min(10, -Math.floor(Math.log10(step)) + 2));
  return Number(value.toFixed(decimals));
}

/** Eksen kademesini kısaltır: 1.500 → "1,5B", 2.300.000 → "2,3M". */
export function compactNumber(value: number, locale = 'tr-TR'): string {
  const abs = Math.abs(value);
  const sign = value < 0 ? '-' : '';
  const fmt = (v: number, digits: number) =>
    new Intl.NumberFormat(locale, { maximumFractionDigits: digits }).format(v);

  if (abs >= 1_000_000) return `${sign}${fmt(abs / 1_000_000, 1)}M`;
  if (abs >= 1_000)     return `${sign}${fmt(abs / 1_000, abs >= 10_000 ? 0 : 1)}B`;
  return `${sign}${fmt(abs, 0)}`;
}

/**
 * Yuvarlatılmış veri ucu olan sütun yolu: taban tarafı köşeli, veri ucu 4px
 * yuvarlak. `rect rx` ile çizilse dört köşe de yuvarlanır ve sütun tabandan
 * kopmuş gibi görünür.
 */
export function columnPath(
  x: number, width: number, baseline: number, tip: number, radius = 4,
): string {
  const up = tip <= baseline;
  const height = Math.abs(baseline - tip);
  const r = Math.max(0, Math.min(radius, width / 2, height));
  if (height === 0) return '';

  return up
    ? `M ${x} ${baseline} L ${x} ${tip + r} Q ${x} ${tip} ${x + r} ${tip} ` +
      `L ${x + width - r} ${tip} Q ${x + width} ${tip} ${x + width} ${tip + r} ` +
      `L ${x + width} ${baseline} Z`
    : `M ${x} ${baseline} L ${x} ${tip - r} Q ${x} ${tip} ${x + r} ${tip} ` +
      `L ${x + width - r} ${tip} Q ${x + width} ${tip} ${x + width} ${tip - r} ` +
      `L ${x + width} ${baseline} Z`;
}
