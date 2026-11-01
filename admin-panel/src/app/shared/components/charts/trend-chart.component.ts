import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef,
  NgZone, OnDestroy, computed, inject, input, signal,
} from '@angular/core';
import { CHART_INK, ChartPoint, compactNumber, niceTicks } from './chart.model';

/**
 * Zaman içindeki tek bir ölçünün seyri: 2px çizgi + %10 opaklıkta alan yıkaması.
 *
 * Tek seri olduğu için lejant yok — kartın başlığı neyin çizildiğini söylüyor.
 * Değerler üç yoldan okunabiliyor: en yüksek/en son noktanın doğrudan etiketi,
 * eksen kademeleri ve tablo ikizi. Tooltip yalnızca kolaylık; hiçbir değerin
 * tek erişim yolu değil.
 */
@Component({
  selector: 'app-trend-chart',
  standalone: true,
  templateUrl: './trend-chart.component.html',
  styleUrl: './trend-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TrendChartComponent implements AfterViewInit, OnDestroy {
  points     = input<ChartPoint[]>([]);
  height     = input(210);
  /** Tooltip, doğrudan etiket ve tabloda kullanılan tam biçim (para birimi vb.). */
  format     = input<(value: number) => string>((v) => String(v));
  /** Eksen kademesi biçimi — kısaltılmış olmalı, yoksa etiketler çakışır. */
  axisFormat = input<(value: number) => string>((v) => compactNumber(v));
  /** Tablo ikizindeki değer kolonunun başlığı; aria etiketinde de geçiyor. */
  seriesName = input('Değer');
  /** Tablodaki üçüncü kolonun başlığı (ChartPoint.note doluysa). */
  noteHeader = input('');
  emptyText  = input('Bu dönemde veri yok');
  /** Veri yenilenirken önceki çizimi soluk tut — iskelet yanıp sönmesin. */
  stale      = input(false);

  readonly ink = CHART_INK;
  readonly PAD = { l: 50, r: 16, t: 20, b: 24 };

  width  = signal(0);
  active = signal(-1);

  private readonly host = inject<ElementRef<HTMLElement>>(ElementRef);
  private readonly zone = inject(NgZone);
  private observer?: ResizeObserver;

  ngAfterViewInit(): void {
    const el = this.host.nativeElement;
    this.width.set(el.clientWidth);

    if (typeof ResizeObserver === 'undefined') return;
    this.observer = new ResizeObserver((entries) => {
      const w = entries[0]?.contentRect.width ?? 0;
      // Genişlik değişimi Angular'ın dışından gelebiliyor; sinyali zone içinde
      // güncellemek değişiklik algılamasının tetiklenmesini garanti ediyor.
      this.zone.run(() => this.width.set(Math.round(w)));
    });
    this.observer.observe(el);
  }

  ngOnDestroy(): void {
    this.observer?.disconnect();
  }

  readonly geo = computed(() => {
    const pts = this.points();
    const w   = Math.max(this.width() || 320, 260);
    const h   = this.height();
    const iw  = Math.max(w - this.PAD.l - this.PAD.r, 20);
    const ih  = Math.max(h - this.PAD.t - this.PAD.b, 20);

    const max   = pts.reduce((m, p) => Math.max(m, p.value), 0);
    const ticks = niceTicks(max, 4);
    const yMax  = ticks[ticks.length - 1] || 1;

    const baseline = this.PAD.t + ih;
    const xAt = (i: number) =>
      pts.length <= 1 ? this.PAD.l + iw / 2 : this.PAD.l + (i / (pts.length - 1)) * iw;
    const yAt = (v: number) => baseline - (v / yMax) * ih;

    const coords = pts.map((p, i) => ({ ...p, cx: xAt(i), cy: yAt(p.value), index: i }));

    const linePath = coords
      .map((c, i) => `${i === 0 ? 'M' : 'L'} ${c.cx.toFixed(1)} ${c.cy.toFixed(1)}`)
      .join(' ');

    const areaPath = coords.length
      ? `M ${coords[0].cx.toFixed(1)} ${baseline} ` +
        coords.map((c) => `L ${c.cx.toFixed(1)} ${c.cy.toFixed(1)}`).join(' ') +
        ` L ${coords[coords.length - 1].cx.toFixed(1)} ${baseline} Z`
      : '';

    // Eksen etiketleri seyreltilir; son etiket her zaman gösterilir ama
    // kendinden öncekiyle çakışacak kadar yakınsa o önceki atılır.
    // Eksen etiketi sayısı genişliğe göre: telefonda 3-4, masaüstünde 6-7.
    type Coord = (typeof coords)[number];
    const maxLabels = Math.max(2, Math.floor(iw / 74));
    const stride  = Math.max(1, Math.ceil(coords.length / maxLabels));
    const xLabels: Coord[] = coords.filter((_, i) => i % stride === 0);
    const last: Coord | undefined = coords[coords.length - 1];
    if (last && xLabels[xLabels.length - 1] !== last) {
      const previous = xLabels[xLabels.length - 1];
      if (previous && last.cx - previous.cx < 42) xLabels.pop();
      xLabels.push(last);
    }

    // Doğrudan etiket tek bir noktada: zirve. Dönemin toplamı ve ortalaması
    // zaten üstteki kartlarda; son değeri de yazdırmak etiketi çizginin
    // üstüne oturtuyor ve iki sayı birbiriyle yarışıyordu.
    let peak: Coord | null = null;
    for (const c of coords) if (!peak || c.value > peak.value) peak = c;

    const labelled: Coord[] = peak && peak.value > 0 ? [peak] : [];

    return {
      w, h, iw, ih, baseline, yMax, coords, linePath, areaPath, xLabels, labelled,
      ticks: ticks.map((v) => ({ v, y: yAt(v), label: this.axisFormat()(v) })),
      hit: { x: this.PAD.l, y: this.PAD.t, w: iw, h: ih },
    };
  });

  readonly endPoint = computed(() => {
    const c = this.geo().coords;
    return c.length ? c[c.length - 1] : null;
  });

  readonly activePoint = computed(() => {
    const i = this.active();
    const c = this.geo().coords;
    return i >= 0 && i < c.length ? c[i] : null;
  });

  readonly ariaLabel = computed(() => {
    const pts = this.points();
    if (!pts.length) return this.emptyText();
    return `${this.seriesName()} seyri, ${pts.length} nokta. ` +
           `${pts[0].fullLabel} – ${pts[pts.length - 1].fullLabel}. ` +
           `Değerlerin tamamı grafiğin altındaki tabloda.`;
  });

  onMove(event: PointerEvent): void {
    const rect = (event.currentTarget as SVGRectElement).getBoundingClientRect();
    const n = this.points().length;
    if (!n || rect.width <= 0) return;

    const ratio = (event.clientX - rect.left) / rect.width;
    const index = n === 1 ? 0 : Math.round(ratio * (n - 1));
    this.active.set(Math.min(n - 1, Math.max(0, index)));
  }

  /** Klavyede ok tuşlarıyla aynı okuma — tooltip yalnızca fareye bağlı olmamalı. */
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

  /** Tooltip'in kart dışına sarkmaması için yatay konumu kenarlardan kısıtla. */
  tooltipLeft(cx: number): number {
    return Math.min(Math.max(cx, 64), this.geo().w - 64);
  }
}
