import { formatAmount } from '../../core/pricing';
import { PackageStatus } from '../../core/models/package.model';

/** ₺1.250,50 — kuruş varsa gösterilir. */
export function money(value: number): string {
  return `₺${formatAmount(value)}`;
}

const TZ = 'Europe/Istanbul';

/** 13 Kas 2026 */
export function shortDate(iso?: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleDateString('tr-TR', { day: 'numeric', month: 'short', year: 'numeric', timeZone: TZ });
}

/** 13 Kas 2026, 14:30 */
export function dateTime(iso?: string | null): string {
  if (!iso) return '—';
  return new Date(iso).toLocaleString('tr-TR', {
    day: 'numeric', month: 'short', year: 'numeric', hour: '2-digit', minute: '2-digit', timeZone: TZ,
  });
}

/** "bugün", "dün", "5 gün önce", eskiyse tarih. */
export function relativeDay(iso?: string | null): string {
  if (!iso) return '—';
  const day = (d: Date) => new Date(d.toLocaleDateString('en-CA', { timeZone: TZ })).getTime();
  const diff = Math.round((day(new Date()) - day(new Date(iso))) / 86_400_000);
  if (diff <= 0) return 'bugün';
  if (diff === 1) return 'dün';
  if (diff < 30) return `${diff} gün önce`;
  return shortDate(iso);
}

export function statusLabel(status: PackageStatus, isExpired: boolean): string {
  if (status === 'Cancelled') return 'İptal';
  if (status === 'Completed') return 'Tamamlandı';
  return isExpired ? 'Süresi doldu' : 'Aktif';
}
