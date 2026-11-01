import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef,
  NgZone, OnDestroy, computed, inject, input, signal,
} from '@angular/core';
import { CHART_INK, columnPath, compactNumber, niceTicks } from './chart.model';

/** Bir x değerinde üst üste yığılan parçalar. */
export interface StackedPoint {
  label: string;
  fullLabel: string;
  /** Seri sırasına göre değerler; alttan üste doğru yığılır. */
  values: number[];
}

/**
 * Bir bütünün zaman içinde nasıl bölündüğünü gösteren yığılmış sütunlar
 * (yeni / tekrar gelen müşteri gibi).
 *
 * Parçalar arasında 2px zemin boşluğu var: komşu blokları çerçeveyle değil
 * boşlukla ayırmak, grafiğe veri olmayan mürekkep eklemiyor. İki seri olduğu
 * için lejant her zaman açık — kimlik yalnızca renge bırakılmıyor.
 */
@Component({
  selector: 'app-stacked-bar-chart',
  standalone: true,
  templateUrl: './stacked-bar-chart.component.html',
  styleUrl: './stacked-bar-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class StackedBarChartComponent implements AfterViewInit, OnDestroy {
  points       = input<StackedPoint[]>([]);
  seriesNames  = input<string[]>([]);
  /**
   * Seri renkleri. Varsayılan çift mor + turuncu: marka moruyla mavi,
   * en yaygın renk körlüğünde neredeyse aynı renge düşüyor (OKLab ΔE 0.4),
   * turuncuyla arası ise 32.
   */
  seriesColors = input<string[]>([CHART_INK.series, CHART_INK.accent]);
  height       = input(220);
  format       = input<(value: number) => string>((v) => String(v));
  axisFormat   = input<(value: number) => string>((v) => compactNumber(v));
  integerAxis  = input(false);
  emptyText    = input('Bu dönemde veri yok');
  totalLabel   = input('Toplam');
  stale        = input(false);

  readonly ink = CHART_INK;
  readonly PAD = { l: 50, r: 16, t: 20, b: 24 };

  width  = signal(0);
  active = signal(-1);

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
    const pts = this.points();
    const colors = this.seriesColors();
    const w  = Math.max(this.width() || 320, 260);
    const h  = this.height();
    const iw = Math.max(w - this.PAD.l - this.PAD.r, 20);
    const ih = Math.max(h - this.PAD.t - this.PAD.b, 20);

    const totals = pts.map((p) => p.values.reduce((s, v) => s + v, 0));
    const max    = totals.reduce((m, v) => Math.max(m, v), 0);
    const ticks  = niceTicks(max, 4, this.integerAxis());
    const top    = ticks[ticks.length - 1] || 1;

    const baseline = this.PAD.t + ih;
    const yAt = (v: number) => baseline - (v / top) * ih;

    // Sütun kipindekiyle aynı kural: kalınlık 24px'i, band 88px'i geçmiyor;
    // az sütunlu grafik kartın ortasında toplanıyor.
    const band = Math.min(iw / Math.max(pts.length, 1), 88);
    const barW = Math.max(2, Math.min(24, band - 2));
    const used = band * pts.length;
    const left = this.PAD.l + Math.max(0, (iw - used) / 2);

    const bars = pts.map((p, i) => {
      const bandX = left + i * band;
      const x = bandX + (band - barW) / 2;

      const lastFilled  = p.values.reduce((last, v, idx) => (v > 0 ? idx : last), -1);
      const firstFilled = p.values.findIndex((v) => v > 0);
      let cumulative = 0;
      const segments: { d: string; color: string; seriesIndex: number }[] = [];

      p.values.forEach((value, seriesIndex) => {
        if (value <= 0) { return; }
        const segBottom = yAt(cumulative);
        const segTop    = yAt(cumulative + value);
        cumulative += value;

        // Alttaki parçayla arasına 2px zemin boşluğu: en alttaki hariç her
        // parçanın tabanı 2px yukarı çekiliyor.
        const bottom = seriesIndex === firstFilled ? segBottom : segBottom - 2;
        if (bottom - segTop <= 0.5) { return; }

        segments.push({
          seriesIndex,
          color: colors[seriesIndex] ?? CHART_INK.series,
          // Yalnızca en üstteki dolu parçanın ucu yuvarlak; aradakiler köşeli,
          // yoksa sütun birbirine geçmiş kapsüllere dönüşüyor.
          d: seriesIndex === lastFilled
            ? columnPath(x, barW, bottom, segTop)
            : `M ${x} ${bottom} L ${x} ${segTop} L ${x + barW} ${segTop} L ${x + barW} ${bottom} Z`,
        });
      });

      return {
        ...p,
        index: i,
        bandX, band, centerX: bandX + band / 2,
        total: totals[i],
        tipY: yAt(totals[i]),
        segments,
      };
    });

    const maxLabels = Math.max(2, Math.floor(used / 52));
    const stride = Math.max(1, Math.ceil(bars.length / maxLabels));

    return {
      w, h, iw, ih, bars, baseline,
      left, right: left + used,
      xLabels: bars.filter((_, i) => i % stride === 0),
      ticks: ticks.map((v) => ({ v, y: yAt(v), label: this.axisFormat()(v) })),
    };
  });

  readonly activeBar = computed(() => {
    const i = this.active();
    const b = this.geo().bars;
    return i >= 0 && i < b.length ? b[i] : null;
  });

  readonly ariaLabel = computed(() => {
    const pts = this.points();
    if (!pts.length) return this.emptyText();
    return `${this.seriesNames().join(' ve ')} kırılımı, ${pts.length} sütun. ` +
           `Değerlerin tamamı grafiğin altındaki tabloda.`;
  });

  onKey(event: KeyboardEvent): void {
    const n = this.points().length;
    if (!n) return;
    const current = this.active() < 0 ? 0 : this.active();

    switch (event.key) {
      case 'ArrowRight': this.active.set(Math.min(n - 1, current + 1)); break;
      case 'ArrowLeft':  this.active.set(Math.max(0, current - 1));     break;
      case 'Home':       this.active.set(0);                            break;
      case 'End':        this.active.set(n - 1);                        break;
      case 'Escape':     this.active.set(-1);                           return;
      default: return;
    }
    event.preventDefault();
  }

  tooltipLeft(cx: number): number {
    return Math.min(Math.max(cx, 72), this.geo().w - 72);
  }
}
