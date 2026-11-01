import {
  AfterViewInit, ChangeDetectionStrategy, Component, ElementRef,
  NgZone, OnDestroy, computed, inject, input, signal,
} from '@angular/core';
import {
  CHART_INK, ChartPoint, columnPath, compactNumber, divergingTicks, niceTicks,
} from './chart.model';

/**
 * Büyüklük karşılaştırması için sütunlar. İki kipte çalışıyor:
 *
 * - **tek seri** (varsayılan): bütün sütunlar aynı renk. Sütunu değerine göre
 *   koyulaştırmak, boyun zaten gösterdiği bilgiyi renge de yüklemek olurdu.
 * - **ıraksak** (`diverging`): işaret anlam taşıyor — sıfırın üstü mavi (kâr),
 *   altı kırmızı (zarar), tabanda nötr bir sıfır çizgisi. Renk tek başına
 *   kalmasın diye değerler işaretiyle (+/−) yazılıyor.
 */
@Component({
  selector: 'app-bar-chart',
  standalone: true,
  templateUrl: './bar-chart.component.html',
  styleUrl: './bar-chart.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BarChartComponent implements AfterViewInit, OnDestroy {
  points     = input<ChartPoint[]>([]);
  height     = input(210);
  format     = input<(value: number) => string>((v) => String(v));
  axisFormat = input<(value: number) => string>((v) => compactNumber(v));
  /**
   * Sütunun ucuna yazılan etiketin biçimi. Verilmezse `format` kullanılır;
   * tooltipte "11 randevu" uygunken grafiğin üstünde sadece "11" durmalıysa
   * ayrıştırmak için var.
   */
  labelFormat = input<((value: number) => string) | null>(null);
  /** Tam sayı ekseni: kademeler 2,5 gibi kesirli değerlere düşmez. */
  integerAxis = input(false);
  /** Sıfırın iki yanına uzanan eksen: üstü kâr, altı zarar. */
  diverging  = input(false);
  /**
   * Tek seri kipinde sütun rengi. Varsayılan marka moru; giderde olduğu gibi
   * sayfanın geri kalanı başka bir renkle anlam kurduğunda o renk geçilir.
   * Değerine göre koyulaşan bir rampa verilmez — bu, boyun zaten gösterdiği
   * bilgiyi renge de yüklemek olur.
   */
  color      = input<string>(CHART_INK.series);
  seriesName = input('Değer');
  noteHeader = input('');
  emptyText  = input('Bu dönemde veri yok');
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

    const values = pts.map((p) => p.value);
    const max = values.reduce((m, v) => Math.max(m, v), 0);
    const min = values.reduce((m, v) => Math.min(m, v), 0);

    const ticks = this.diverging() && min < 0
      ? divergingTicks(min, max, 4)
      : niceTicks(max, 4, this.integerAxis());

    const top    = ticks[ticks.length - 1] || 1;
    const bottom = ticks[0] < 0 ? ticks[0] : 0;
    const span   = top - bottom || 1;

    const yAt = (v: number) => this.PAD.t + ih - ((v - bottom) / span) * ih;
    const zeroY = yAt(0);

    // Sütun kalınlığı 24px'i geçmiyor; bandın artanı hava olarak kalıyor.
    // Komşu sütunlar arasında 2px'lik zemin boşluğu bırakılıyor.
    const band = iw / Math.max(pts.length, 1);
    const barW = Math.max(2, Math.min(24, band - 2));

    const bars = pts.map((p, i) => {
      const bandX = this.PAD.l + i * band;
      const x = bandX + (band - barW) / 2;
      const tipY = yAt(p.value);
      return {
        ...p,
        index: i,
        x, barW, bandX, band,
        centerX: bandX + band / 2,
        tipY,
        path: columnPath(x, barW, zeroY, tipY),
        color: this.diverging() && p.value < 0 ? this.ink.negative
             : this.diverging()                ? this.ink.positive
             :                                   this.color(),
        // Değer etiketi, negatif sütunlarda ucun altına yazılır.
        labelY: p.value < 0 ? tipY + 13 : tipY - 7,
      };
    });

    // Doğrudan etiket seyrek: en yüksek sütun, ıraksak kipte ayrıca en düşüğü.
    // Her sütuna sayı yazmak grafiği okunmaz hâle getiriyor — gerisi eksende,
    // tooltipte ve tabloda duruyor.
    let peak: (typeof bars)[number] | null = null;
    let trough: (typeof bars)[number] | null = null;
    for (const b of bars) {
      if (!peak   || b.value > peak.value)   peak = b;
      if (!trough || b.value < trough.value) trough = b;
    }
    const labelled = [
      peak && peak.value > 0 ? peak : null,
      this.diverging() && trough && trough.value < 0 ? trough : null,
    ].filter((b): b is (typeof bars)[number] => !!b);

    // Eksen etiketi sayısı bandın genişliğine göre: dar ekranda 3-4 etiket
    // kalıyor, geniş ekranda 12'ye kadar çıkıyor. Sabit bir sayı telefonda
    // etiketleri üst üste bindiriyordu.
    const maxLabels = Math.max(2, Math.floor(iw / 52));
    const stride = Math.max(1, Math.ceil(bars.length / maxLabels));
    const xLabels = bars.filter((_, i) => i % stride === 0);

    return {
      w, h, iw, ih, bars, zeroY, labelled, xLabels,
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
    return `${this.seriesName()}, ${pts.length} sütun. ` +
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

  /** Sütun ucundaki etiket: `labelFormat` verilmemişse `format`. */
  barLabel(value: number): string {
    const fn = this.labelFormat() ?? this.format();
    return `${this.diverging() && value > 0 ? '+' : ''}${fn(value)}`;
  }

  tooltipLeft(cx: number): number {
    return Math.min(Math.max(cx, 64), this.geo().w - 64);
  }

  /** Negatif sütunlarda tooltip ucun altında durur, grafiğin içinde kalır. */
  tooltipTop(bar: { value: number; tipY: number }): number {
    return bar.value < 0 ? bar.tipY + 42 : bar.tipY - 12;
  }
}
