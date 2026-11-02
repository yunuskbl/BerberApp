import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef,
  NgZone, OnDestroy, computed, inject, input, signal,
} from '@angular/core';
import { CHART_INK, HEAT_RAMP, HeatCell } from './chart.model';

/**
 * Izgara üzerinde büyüklük: gün × saat yoğunluğu gibi.
 *
 * Renk işi sequential — tek hue, açıktan koyuya. Kategorik palet kullanılmıyor:
 * hücreler arasındaki fark kimlik değil miktar. Değeri olmayan hücre hiç
 * boyanmıyor ki "randevu yok" ile "az randevu" aynı görünmesin.
 *
 * Her değer üç yoldan okunabiliyor: hücre tooltip'i, ölçek açıklaması ve
 * tablo ikizi.
 */
@Component({
  selector: 'app-heatmap-chart',
  standalone: true,
  templateUrl: './heatmap-chart.component.html',
  styleUrl: './heatmap-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HeatmapChartComponent implements AfterViewInit, OnDestroy {
  cells      = input<HeatCell[]>([]);
  /** Satır başlıkları (örn. Pzt…Paz). */
  rowLabels  = input<string[]>([]);
  /** Sütun başlıkları (örn. 09, 10, 11…). */
  colLabels  = input<string[]>([]);
  format     = input<(value: number) => string>((v) => String(v));
  emptyText  = input('Bu dönemde veri yok');
  /** Ölçek açıklamasının iki ucundaki metin. */
  lowLabel   = input('az');
  highLabel  = input('çok');
  stale      = input(false);

  readonly ink  = CHART_INK;
  readonly ramp = HEAT_RAMP;
  /** Satır başlığı sütunu + üstteki saat şeridi. */
  readonly PAD = { l: 42, t: 20, r: 4, b: 4 };

  width  = signal(0);
  active = signal<string | null>(null);

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly zone = inject(NgZone);
  private observer?: ResizeObserver;

  ngAfterViewInit(): void {
    this.width.set(this.host.nativeElement.clientWidth);
    if (typeof ResizeObserver === 'undefined') return;
    this.observer = new ResizeObserver((entries) => {
      const w = entries[0]?.contentRect.width ?? 0;
      this.zone.run(() => this.width.set(Math.round(w)));
    });
    this.observer.observe(this.host.nativeElement);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  readonly geo = computed(() => {
    const cells = this.cells();
    const rows  = this.rowLabels();
    const cols  = this.colLabels();
    const w     = Math.max(this.width() || 320, 260);

    const gridW = Math.max(w - this.PAD.l - this.PAD.r, 60);
    // Hücre genişliği 52px'i geçmiyor: geniş kartta 12 saati tüm genişliğe
    // yaymak hücreleri yatık dikdörtgenlere çeviriyor ve ızgara, ısı
    // haritasından çok yığılmış sütun grafiğine benziyor. Artan boşlukta
    // ızgara ortalanıyor.
    const rawW  = cols.length > 0 ? gridW / cols.length : gridW;
    const cellW = Math.min(rawW, 52);
    // Hücreler kareye yakın duruyor; dar ekranda 14px'in altına inilmiyor,
    // daha küçüğünde dokunmatik hedef kalmıyor.
    const cellH = Math.max(14, Math.min(cellW, 38));
    const used  = cellW * cols.length;
    const left  = this.PAD.l + Math.max(0, (gridW - used) / 2);
    const h     = this.PAD.t + rows.length * cellH + this.PAD.b;

    const max = cells.reduce((m, c) => Math.max(m, c.value), 0);

    const painted = cells
      .filter((c) => c.value > 0 && c.row < rows.length && c.col < cols.length)
      .map((c) => {
        // Beş kademeye bölünüyor; en yüksek değer her zaman en koyu adımda.
        const ratio = max > 0 ? c.value / max : 0;
        const step  = Math.min(HEAT_RAMP.length - 1, Math.max(0, Math.ceil(ratio * HEAT_RAMP.length) - 1));
        return {
          ...c,
          key: `${c.row}-${c.col}`,
          // 2px zemin boşluğu: komşu hücreleri çerçeveyle değil boşlukla ayır.
          x: left + c.col * cellW + 1,
          y: this.PAD.t + c.row * cellH + 1,
          w: Math.max(1, cellW - 2),
          h: Math.max(1, cellH - 2),
          centerX: left + c.col * cellW + cellW / 2,
          centerY: this.PAD.t + c.row * cellH + cellH / 2,
          color: HEAT_RAMP[step],
        };
      });

    // Saat şeridi kalabalıksa seyreltilir.
    const colStride = Math.max(1, Math.ceil(cols.length / Math.max(2, Math.floor(used / 30))));

    return {
      w, h, cellW, cellH, max, painted, left,
      rows: rows.map((label, i) => ({
        label, i, y: this.PAD.t + i * cellH + cellH / 2,
      })),
      cols: cols.map((label, i) => ({
        label, i, x: left + i * cellW + cellW / 2,
        show: i % colStride === 0,
      })),
    };
  });

  readonly activeCell = computed(() => {
    const key = this.active();
    return key ? this.geo().painted.find((c) => c.key === key) ?? null : null;
  });

  readonly ariaLabel = computed(() => {
    const cells = this.cells();
    if (!cells.length) return this.emptyText();
    return `Gün ve saate göre yoğunluk ısı haritası, ${cells.length} dolu hücre. ` +
           `Değerlerin tamamı grafiğin altındaki tabloda.`;
  });

  /** Tablo ikizi: en yoğun hücreden başlayarak. */
  readonly tableRows = computed(() =>
    [...this.geo().painted].sort((a, b) => b.value - a.value).slice(0, 25));

  tooltipLeft(cx: number): number {
    return Math.min(Math.max(cx, 80), this.geo().w - 80);
  }
}
