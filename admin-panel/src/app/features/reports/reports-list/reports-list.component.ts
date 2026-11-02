import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import {
  EarningsService, EarningsDto, PriceDifferenceDetailDto, CustomerInsightsDto,
  PeakSlotDto,
} from '../../../core/services/earnings.service';
import { ExpenseService, ExpenseDto } from '../../../core/services/expense.service';
import { LanguageService } from '../../../core/services/language.service';
import { AuthService } from '../../../core/services/auth.service';
import { BranchService } from '../../../core/services/branch.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { TrendChartComponent } from '../../../shared/components/charts/trend-chart.component';
import { BarChartComponent } from '../../../shared/components/charts/bar-chart.component';
import {
  StackedBarChartComponent, StackedPoint,
} from '../../../shared/components/charts/stacked-bar-chart.component';
import { HeatmapChartComponent } from '../../../shared/components/charts/heatmap-chart.component';
import {
  CHART_INK, ChartPoint, HeatCell, compactNumber,
} from '../../../shared/components/charts/chart.model';

/** Önceki döneme göre değişim — yön ve bunun iyi mi kötü mü olduğu. */
interface PeriodDelta {
  percent: number;
  direction: 'up' | 'down' | 'flat';
  good: boolean;
}
import { planAtLeast } from '../../../core/plans';
import { environment } from '../../../../environments/environment';

interface BranchOption { id: string; name: string; }

/** Gün bazında birleştirilmiş gelir/gider satırı — grafiklerin ham girdisi. */
interface DayRow {
  date:    Date;
  key:     string;   // YYYY-MM-DD
  income:  number;   // TRY
  count:   number;   // randevu
  expense: number;   // TRY
}

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [
    CommonModule, FormsModule, ReactiveFormsModule, TranslatePipe,
    TrendChartComponent, BarChartComponent, StackedBarChartComponent,
    HeatmapChartComponent,
  ],
  templateUrl: './reports-list.component.html',
  styleUrl: './reports-list.component.scss'
})
export class ReportsListComponent implements OnInit {
  earnings: EarningsDto | null = null;
  isLoading = false;

  // ── Kapsam seçici (Premium + ana bağlam) ─────────────────────────────────
  branches: BranchOption[] = [];
  selectedScope: 'main' | 'branch' | 'consolidated' = 'main';
  selectedBranchId: string | null = null;

  get isPremium(): boolean {
    return planAtLeast(this.authService.getUserPlan(), 'Premium');
  }

  get showScopeSelector(): boolean {
    return this.isPremium && !this.branchService.activeBranch() && this.branches.length > 0;
  }

  get scopeLabel(): string {
    if (this.selectedScope === 'consolidated') return 'Tüm Şubeler';
    if (this.selectedScope === 'branch') return this.branches.find(b => b.id === this.selectedBranchId)?.name ?? '';
    return 'Ana İşletme';
  }

  setScope(scope: 'main' | 'branch' | 'consolidated', branchId?: string): void {
    this.selectedScope = scope;
    this.selectedBranchId = branchId ?? null;
    this.loadEarnings();
    if (this.insights) this.loadCustomerInsights();
  }

  onScopeChange(event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    if (value === 'main') {
      this.setScope('main');
    } else if (value === 'consolidated') {
      this.setScope('consolidated');
    } else if (value.startsWith('branch:')) {
      this.setScope('branch', value.slice(7));
    }
  }

  // ── Fiyat farkı modal ────────────────────────────────────────────────────
  showPriceDiffModal = false;

  // ── Aktif sekme ───────────────────────────────────────────────────────────
  activeTab: 'income' | 'expense' | 'customers' = 'income';

  // ── Tarih filtresi ────────────────────────────────────────────────────────
  reportStartDate = this.getDateString(new Date(new Date().setDate(new Date().getDate() - 30)));
  reportEndDate = this.getDateString(new Date());

  // ── Gider yönetimi ────────────────────────────────────────────────────────
  expenses: ExpenseDto[] = [];
  isExpensesLoading = false;
  expenseForm!: FormGroup;
  isExpenseFormOpen = false;
  editingExpense: ExpenseDto | null = null;
  isSavingExpense = false;
  expenseError = '';
  expenseSuccess = '';

  readonly expenseCategories = [
    'Kira', 'Elektrik', 'Su', 'Doğalgaz', 'İnternet',
    'Malzeme & Ürün', 'Ekipman', 'Personel Maaşı',
    'Reklam & Pazarlama', 'Vergi & Harç', 'Sigorta', 'Diğer'
  ];
  readonly currencies = ['TRY', 'USD', 'EUR', 'GBP'];

  constructor(
    private earningsService: EarningsService,
    private expenseService: ExpenseService,
    private fb: FormBuilder,
    public langService: LanguageService,
    private authService: AuthService,
    public branchService: BranchService,
    private http: HttpClient,
  ) {}

  ngOnInit(): void {
    this.loadEarnings();
    this.loadExpenses();
    this.initExpenseForm();
    if (this.isPremium && !this.branchService.activeBranch()) {
      this.loadBranches();
    }
  }

  loadBranches(): void {
    this.http.get<any>(`${environment.apiUrl}/tenants/branches`).subscribe({
      next: (res) => {
        if (res.success) this.branches = res.data.map((b: any) => ({ id: b.id, name: b.name }));
      },
      error: () => {}
    });
  }

  // ── Gelir ─────────────────────────────────────────────────────────────────

  loadEarnings(): void {
    this.isLoading = true;
    const branchId = this.selectedScope === 'branch' ? this.selectedBranchId ?? undefined : undefined;
    const consolidated = this.selectedScope === 'consolidated';
    this.earningsService.getEarnings(
      this.reportStartDate, this.reportEndDate, undefined, branchId, consolidated
    ).subscribe({
      next: (res) => {
        if (res.success) this.earnings = res.data;
        this.rebuildCharts();
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  // ── Gider ─────────────────────────────────────────────────────────────────

  loadExpenses(): void {
    this.isExpensesLoading = true;
    this.expenseService.getExpenses(this.reportStartDate, this.reportEndDate).subscribe({
      next: (res) => {
        if (res.success) this.expenses = res.data;
        this.isExpensesLoading = false;
      },
      error: () => { this.isExpensesLoading = false; }
    });
  }

  initExpenseForm(expense?: ExpenseDto): void {
    this.expenseForm = this.fb.group({
      date:        [expense?.date ?? this.getDateString(new Date()), Validators.required],
      amount:      [expense?.amount ?? null, [Validators.required, Validators.min(0.01)]],
      currency:    [expense?.currency ?? 'TRY', Validators.required],
      category:    [expense?.category ?? ''],
      description: [expense?.description ?? ''],
      note:        [expense?.note ?? ''],
    });
  }

  openExpenseForm(expense?: ExpenseDto): void {
    this.editingExpense = expense ?? null;
    this.expenseError = '';
    this.expenseSuccess = '';
    this.initExpenseForm(expense);
    this.isExpenseFormOpen = true;
  }

  closeExpenseForm(): void {
    this.isExpenseFormOpen = false;
    this.editingExpense = null;
  }

  saveExpense(): void {
    if (this.expenseForm.invalid) return;
    this.isSavingExpense = true;
    this.expenseError = '';

    const val = this.expenseForm.value;

    const req = {
      date:        val.date,
      amount:      Number(val.amount),
      currency:    val.currency || 'TRY',
      category:    val.category || '',
      description: val.description || undefined,
      note:        val.note || undefined,
    };

    const action$ = this.editingExpense
      ? this.expenseService.updateExpense(this.editingExpense.id, req)
      : this.expenseService.createExpense(req);

    action$.subscribe({
      next: () => {
        this.isSavingExpense = false;
        this.closeExpenseForm();
        this.loadExpenses();
        this.loadEarnings(); // P&L güncellenir
        this.expenseSuccess = this.editingExpense ? 'Gider güncellendi.' : 'Gider eklendi.';
        setTimeout(() => this.expenseSuccess = '', 3000);
      },
      error: (err) => {
        this.isSavingExpense = false;
        this.expenseError = err?.error?.message || 'Gider kaydedilemedi.';
      }
    });
  }

  deleteExpense(id: string): void {
    if (!confirm('Bu gideri silmek istediğinizden emin misiniz?')) return;
    this.expenseService.deleteExpense(id).subscribe({
      next: () => {
        this.loadExpenses();
        this.loadEarnings();
      }
    });
  }

  get totalExpensesInPeriod(): number {
    return this.expenses.reduce((sum, e) => sum + e.amount, 0);
  }

  get positivePriceDiffTotal(): number {
    return (this.earnings?.priceDifferences ?? [])
      .filter(d => d.difference > 0)
      .reduce((sum, d) => sum + d.difference, 0);
  }

  get negativePriceDiffTotal(): number {
    return (this.earnings?.priceDifferences ?? [])
      .filter(d => d.difference < 0)
      .reduce((sum, d) => sum + d.difference, 0);
  }

  // ── Grafikler ─────────────────────────────────────────────────────────────
  //
  // Hepsi aynı tarih filtresinden besleniyor; grafik başına ayrı bir filtre yok.
  // Veri zaten API yanıtında geliyordu (daily / expenseDaily), tabloların altında
  // görünmüyordu.

  /** Seçili aralığın uzunluğuna göre gün / hafta / ay kırılımı. */
  chartBucket: 'day' | 'week' | 'month' = 'day';
  incomeTrend:  ChartPoint[] = [];
  apptTrend:    ChartPoint[] = [];
  netTrend:     ChartPoint[] = [];
  expenseTrend: ChartPoint[] = [];
  weekdayBars:  ChartPoint[] = [];
  /** Dönemde hiç hareket yoksa grafik kartları hiç basılmaz. */
  hasChartData = false;

  get chartBucketLabel(): string {
    return this.chartBucket === 'day' ? 'günlük'
         : this.chartBucket === 'week' ? 'haftalık' : 'aylık';
  }

  /** Net kâr grafiği yalnızca dönemde gider varsa anlamlı. */
  get showNetChart(): boolean {
    return this.netTrend.length > 0 && (this.earnings?.totalExpenses ?? 0) > 0;
  }

  /** Haftanın günleri kırılımı en az bir tam hafta görünce anlam kazanıyor. */
  showWeekdayChart = false;

  /** Gider grafiği, sayfanın geri kalanıyla aynı kırmızıyı giyiyor. */
  readonly expenseColor = CHART_INK.negative;

  // ── Yoğunluk ve randevu durumları ─────────────────────────────────────────

  /** Gün × saat ısı haritası. */
  heatCells: HeatCell[] = [];
  heatRows: string[] = [];
  heatCols: string[] = [];
  /** En yoğun saat dilimleri ve orada uygulanan hizmetler. */
  peakSlots: PeakSlotDto[] = [];
  /**
   * Tamamlanmayan randevuların kırılımı. "Tamamlandı" bilerek dışarıda:
   * 168'e karşı 14 ve 6 çizdirildiğinde küçük sütunlar birkaç piksele
   * iniyor ve asıl karşılaştırma (iptal mi, gelmedi mi) okunamıyor.
   * Tamamlanan sayısı zaten sayfanın her yerinde; buradaki soru "ne oldu da
   * olmadı".
   */
  statusBars: ChartPoint[] = [];
  /** Dönemde hiç sorunlu randevu yoksa grafik yerine olumlu bir not basılır. */
  get hasIncompleteAppointments(): boolean {
    return this.statusBars.some(b => b.value > 0);
  }

  get completionRate(): number {
    const all = this.earnings?.allAppointments ?? 0;
    if (all === 0) return 0;
    const completed = this.earnings?.statusBreakdown?.find(s => s.status === 'Completed')?.count ?? 0;
    return Math.round((completed / all) * 1000) / 10;
  }

  /** Gün sırası pazartesiden başlıyor; DayOfWeek 0 = pazar. */
  private readonly weekdayOrder = [1, 2, 3, 4, 5, 6, 0];

  get cancelRate(): number {
    const all = this.earnings?.allAppointments ?? 0;
    return all > 0 ? Math.round((this.earnings!.cancelledAppointments / all) * 1000) / 10 : 0;
  }

  get noShowRate(): number {
    const all = this.earnings?.allAppointments ?? 0;
    return all > 0 ? Math.round((this.earnings!.noShowAppointments / all) * 1000) / 10 : 0;
  }

  get hasStatusData(): boolean {
    return (this.earnings?.allAppointments ?? 0) > 0;
  }

  /** Isı haritası ancak birkaç dolu hücre varsa bir şey anlatıyor. */
  get hasHeatData(): boolean {
    return this.heatCells.length >= 3;
  }

  // ── Önceki döneme göre değişim ─────────────────────────────────────────────

  /**
   * Yüzde değişim. Önceki dönem sıfırsa `null` dönüyor: sıfırdan artışı
   * yüzdeyle ifade etmek ("%∞ arttı") bilgi taşımıyor, kartta hiç
   * göstermemek daha dürüst.
   */
  delta(current: number, previous: number, upIsGood = true): PeriodDelta | null {
    if (!previous) return null;

    const percent = Math.round(((current - previous) / Math.abs(previous)) * 100);
    const direction: PeriodDelta['direction'] =
      percent > 0 ? 'up' : percent < 0 ? 'down' : 'flat';

    return {
      percent: Math.abs(percent),
      direction,
      // Giderde artış iyi haber değil; yön aynı, yorum ters.
      good: direction === 'flat' ? true : (direction === 'up') === upIsGood,
    };
  }

  get previousPeriodLabel(): string {
    const e = this.earnings;
    if (!e?.previousStartDate) return 'önceki dönem';
    return `${this.formatDate(e.previousStartDate)} – ${this.formatDate(e.previousEndDate)}`;
  }

  private rebuildStatusAndHeat(): void {
    this.heatCells = [];
    this.heatRows = [];
    this.heatCols = [];
    this.peakSlots = [];
    this.statusBars = [];

    const e = this.earnings;
    if (!e) return;

    this.peakSlots = e.peakSlots ?? [];

    const labels: Record<string, string> = {
      Confirmed: 'Onaylı',
      Pending:   'Bekliyor',
      Cancelled: 'İptal',
      NoShow:    'Gelmedi',
    };
    const order = ['Cancelled', 'NoShow', 'Pending', 'Confirmed'];

    this.statusBars = order
      .map(status => {
        const row = (e.statusBreakdown ?? []).find(s => s.status === status);
        return {
          label: labels[status],
          fullLabel: labels[status],
          value: row?.count ?? 0,
          note: (e.allAppointments ?? 0) > 0
            ? `randevuların %${Math.round((row?.count ?? 0) * 100 / e.allAppointments)}'i`
            : undefined,
        };
      });

    // ── Isı haritası ────────────────────────────────────────────────────────
    const cells = e.peakHours ?? [];
    if (cells.length === 0) return;

    // Saat şeridi yalnızca gerçekten randevu olan saatleri kapsıyor; 00-23
    // arasını baştan çizmek haritanın yarısını boş bırakıyordu.
    const hours = cells.map(c => c.hour);
    const minHour = Math.min(...hours);
    const maxHour = Math.max(...hours);

    this.heatCols = [];
    for (let hour = minHour; hour <= maxHour; hour++) {
      this.heatCols.push(`${hour}`.padStart(2, '0'));
    }

    const weekdayNames = this.weekdayNameList();
    this.heatRows = this.weekdayOrder.map(day => weekdayNames.short[day]);

    this.heatCells = cells.map(c => ({
      row: this.weekdayOrder.indexOf(c.dayOfWeek),
      col: c.hour - minHour,
      value: c.count,
      label: `${weekdayNames.long[c.dayOfWeek]} ${`${c.hour}`.padStart(2, '0')}:00`,
      note: `${this.formatCurrency(Math.round(c.revenue))} gelir`,
    })).filter(c => c.row >= 0);
  }

  private weekdayCache: { locale: string; short: string[]; long: string[] } | null = null;

  /**
   * Gün adları yerel takvime göre; sabit Türkçe dizi yazmak dil seçimini
   * bozardı. Şablondan satır başına çağrıldığı için sonuç önbelleğe alınıyor —
   * her değişiklik algılamasında iki Intl biçimleyici kurmak boşa maliyet.
   */
  private weekdayNameList(): { short: string[]; long: string[] } {
    const locale = this.langService.dateLocale;
    if (this.weekdayCache?.locale === locale) return this.weekdayCache;

    const shortFmt = new Intl.DateTimeFormat(locale, { weekday: 'short' });
    const longFmt  = new Intl.DateTimeFormat(locale, { weekday: 'long' });

    // 4 Ocak 2026 bir pazar; DayOfWeek 0 = pazar ile hizalı.
    const sunday = new Date(2026, 0, 4);
    const short: string[] = [];
    const long: string[] = [];
    for (let day = 0; day < 7; day++) {
      const date = new Date(sunday);
      date.setDate(sunday.getDate() + day);
      short.push(shortFmt.format(date));
      long.push(longFmt.format(date));
    }

    this.weekdayCache = { locale, short, long };
    return this.weekdayCache;
  }

  /** Isı haritası ve dilim tablosunda saat aralığı: "14:00 – 15:00". */
  slotHourLabel(hour: number): string {
    return `${`${hour}`.padStart(2, '0')}:00 – ${`${(hour + 1) % 24}`.padStart(2, '0')}:00`;
  }

  slotDayLabel(dayOfWeek: number): string {
    return this.weekdayNameList().long[dayOfWeek] ?? '';
  }

  // Grafik bileşenlerine girdi olarak geçen biçimleyiciler. Ok işlevi olduğu
  // için `this` bağlamı şablonda da korunuyor.
  // Grafik etiketlerinde kuruş gösterilmiyor: "₺6.448,09" bir sütunun ucunda
  // okunurluğu düşürüyor, tablolarda ise tam değer zaten duruyor.
  readonly moneyFull = (v: number): string => this.formatCurrency(Math.round(v));
  readonly moneyAxis = (v: number): string =>
    `${compactNumber(v, this.langService.dateLocale)} ₺`;
  readonly countFull = (v: number): string =>
    `${new Intl.NumberFormat(this.langService.dateLocale).format(v)} randevu`;
  readonly countAxis = (v: number): string => compactNumber(v, this.langService.dateLocale);

  private rebuildCharts(): void {
    this.incomeTrend  = [];
    this.apptTrend    = [];
    this.netTrend     = [];
    this.expenseTrend = [];
    this.weekdayBars  = [];
    this.hasChartData = false;
    this.showWeekdayChart = false;

    const e = this.earnings;
    if (!e) return;

    // Durum kırılımı ve yoğunluk, gelir grafiklerinden bağımsız kuruluyor:
    // dönemdeki bütün randevular iptal edilmişse gelir grafiği boş kalır ama
    // asıl görülmesi gereken şey tam da o iptallerdir.
    this.rebuildStatusAndHeat();

    const days = this.buildDayRows(e.daily ?? [], e.expenseDaily ?? []);
    if (days.length === 0) return;

    this.hasChartData = days.some(d => d.income > 0 || d.count > 0 || d.expense > 0);
    if (!this.hasChartData) return;

    this.chartBucket = this.bucketForRange(days.length);
    const buckets = this.bucketDays(days, this.chartBucket);

    this.incomeTrend = buckets.map(b => ({
      label: b.label, fullLabel: b.fullLabel, value: b.income,
      note: this.countFull(b.count),
    }));

    this.apptTrend = buckets.map(b => ({
      label: b.label, fullLabel: b.fullLabel, value: b.count,
      note: this.formatCurrency(b.income),
    }));

    this.netTrend = buckets.map(b => ({
      label: b.label, fullLabel: b.fullLabel, value: b.income - b.expense,
      note: `Gelir ${this.formatCurrency(b.income)} · Gider ${this.formatCurrency(b.expense)}`,
    }));

    this.expenseTrend = buckets.map(b => ({
      label: b.label, fullLabel: b.fullLabel, value: b.expense,
    }));

    this.weekdayBars = this.buildWeekdayBars(days);
    this.showWeekdayChart = days.length >= 7;
  }

  /**
   * Aralıktaki her günü üretir — hareketsiz günler sıfırla doldurulur.
   * Yalnızca verisi olan günler çizilirse grafik, kapalı günleri gizleyip
   * yanlış bir süreklilik gösterirdi.
   */
  private buildDayRows(
    daily: { date: string; earnings: number; appointmentCount: number }[],
    expenseDaily: { date: string; total: number }[],
  ): DayRow[] {
    const start = this.parseDayKey(this.reportStartDate);
    const end   = this.parseDayKey(this.reportEndDate);
    if (!start || !end || end < start) return [];

    const income = new Map<string, { value: number; count: number }>();
    for (const d of daily) {
      income.set(this.dayKeyOf(d.date), { value: d.earnings, count: d.appointmentCount });
    }
    const expense = new Map<string, number>();
    for (const x of expenseDaily) {
      expense.set(this.dayKeyOf(x.date), (expense.get(this.dayKeyOf(x.date)) ?? 0) + x.total);
    }

    const rows: DayRow[] = [];
    const cursor = new Date(start);
    // Üst sınır ~10 yıl: elle girilen uç bir tarih aralığı döngüyü kilitlemesin.
    while (cursor <= end && rows.length < 3700) {
      const key = this.toDayKey(cursor);
      const inc = income.get(key);
      rows.push({
        date:    new Date(cursor),
        key,
        income:  inc?.value ?? 0,
        count:   inc?.count ?? 0,
        expense: expense.get(key) ?? 0,
      });
      cursor.setDate(cursor.getDate() + 1);
    }
    return rows;
  }

  /** Aralığın uzunluğu kırılımı belirler; bütün grafikler aynı kırılımı kullanır. */
  private bucketForRange(dayCount: number): 'day' | 'week' | 'month' {
    return dayCount <= 45 ? 'day' : dayCount <= 182 ? 'week' : 'month';
  }

  private formatterCache: {
    locale: string;
    shortDay: Intl.DateTimeFormat; longDay: Intl.DateTimeFormat;
    shortMonth: Intl.DateTimeFormat; longMonth: Intl.DateTimeFormat;
  } | null = null;

  /** Intl biçimleyicileri pahalı; dil değişmedikçe yeniden kurulmuyor. */
  private get dateFormatters() {
    const locale = this.langService.dateLocale;
    if (!this.formatterCache || this.formatterCache.locale !== locale) {
      this.formatterCache = {
        locale,
        shortDay:   new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' }),
        longDay:    new Intl.DateTimeFormat(locale, { weekday: 'short', day: 'numeric', month: 'long' }),
        shortMonth: new Intl.DateTimeFormat(locale, { month: 'short' }),
        longMonth:  new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' }),
      };
    }
    return this.formatterCache;
  }

  /** Bir kovanın ilk/son gününden eksen ve tooltip etiketlerini üretir. */
  private bucketLabels(first: Date, last: Date, bucket: 'day' | 'week' | 'month') {
    const f = this.dateFormatters;
    return {
      label: bucket === 'month' ? f.shortMonth.format(first) : f.shortDay.format(first),
      fullLabel:
        bucket === 'day'  ? f.longDay.format(first)
      : bucket === 'week' ? `${f.shortDay.format(first)} – ${f.shortDay.format(last)}`
      :                     f.longMonth.format(first),
    };
  }

  /** Bir günü ait olduğu kovanın anahtarına çevirir. */
  private bucketKey(day: DayRow | { date: Date; key: string }, bucket: 'day' | 'week' | 'month'): string {
    return bucket === 'day'  ? day.key
         : bucket === 'week' ? this.toDayKey(this.startOfWeek(day.date))
         :                     day.key.slice(0, 7);
  }

  private bucketDays(days: DayRow[], bucket: 'day' | 'week' | 'month') {
    const groups = new Map<string, { first: Date; last: Date; income: number; count: number; expense: number }>();
    for (const d of days) {
      const key = this.bucketKey(d, bucket);
      const g = groups.get(key);
      if (g) {
        g.last = d.date;
        g.income  += d.income;
        g.count   += d.count;
        g.expense += d.expense;
      } else {
        groups.set(key, {
          first: d.date, last: d.date,
          income: d.income, count: d.count, expense: d.expense,
        });
      }
    }

    return [...groups.entries()].map(([key, g]) => ({
      key,
      income: g.income,
      count: g.count,
      expense: g.expense,
      ...this.bucketLabels(g.first, g.last, bucket),
    }));
  }

  /**
   * Haftanın günlerine göre ortalama günlük gelir. Toplam değil ortalama:
   * aralıkta 5 pazartesi ama 4 cumartesi varsa toplamlar karşılaştırılamaz.
   */
  private buildWeekdayBars(days: DayRow[]): ChartPoint[] {
    const locale = this.langService.dateLocale;
    const shortWeekday = new Intl.DateTimeFormat(locale, { weekday: 'short' });
    const longWeekday  = new Intl.DateTimeFormat(locale, { weekday: 'long' });

    const totals = Array.from({ length: 7 }, () => ({ income: 0, count: 0, days: 0 }));
    for (const d of days) {
      const slot = totals[d.date.getDay()];
      slot.income += d.income;
      slot.count  += d.count;
      slot.days   += 1;
    }

    // 1 Ocak 2024 bir pazartesi; hafta pazartesiden başlasın diye referans alındı.
    const reference = new Date(2024, 0, 1);
    const bars: ChartPoint[] = [];
    for (let offset = 0; offset < 7; offset++) {
      const date = new Date(reference);
      date.setDate(reference.getDate() + offset);
      const slot = totals[date.getDay()];
      if (slot.days === 0) continue;

      const average = slot.income / slot.days;
      bars.push({
        label: shortWeekday.format(date),
        fullLabel: longWeekday.format(date),
        value: Math.round(average),
        note: `${slot.days} gün · ${slot.count} randevu · toplam ${this.formatCurrency(slot.income)}`,
      });
    }
    return bars;
  }

  // ── Müşteri takibi ────────────────────────────────────────────────────────

  insights: CustomerInsightsDto | null = null;
  isInsightsLoading = false;
  /** Yeni / tekrar gelen müşteri kırılımı — yığılmış sütunlar. */
  customerMix: StackedPoint[] = [];
  /** Sadakat dağılımı: kaç müşteri kaç kez gelmiş. */
  loyaltyBars: ChartPoint[] = [];
  /** Alttaki parça önce: tekrar gelenler tabanda, yeni müşteriler üstte. */
  readonly customerSeriesNames = ['Tekrar gelen', 'Yeni müşteri'];
  /** Dönemde hiç müşteri hareketi yoksa grafik kartları basılmaz. */
  hasCustomerActivity = false;

  readonly countOnly = (v: number): string =>
    new Intl.NumberFormat(this.langService.dateLocale).format(v);

  /**
   * Müşteri raporu sekmeye ilk girişte yükleniyor. Sayfa açılışında üçüncü bir
   * istek atmak, raporu hiç açmayacak kullanıcıya da bedel ödetirdi.
   */
  setTab(tab: 'income' | 'expense' | 'customers'): void {
    this.activeTab = tab;
    if (tab === 'customers' && !this.insights && !this.isInsightsLoading) {
      this.loadCustomerInsights();
    }
  }

  loadCustomerInsights(): void {
    this.isInsightsLoading = true;
    const branchId = this.selectedScope === 'branch' ? this.selectedBranchId ?? undefined : undefined;
    const consolidated = this.selectedScope === 'consolidated';

    this.earningsService.getCustomerInsights(
      this.reportStartDate, this.reportEndDate, branchId, consolidated
    ).subscribe({
      next: (res) => {
        if (res.success) this.insights = res.data;
        this.rebuildCustomerCharts();
        this.isInsightsLoading = false;
      },
      error: () => { this.isInsightsLoading = false; }
    });
  }

  private rebuildCustomerCharts(): void {
    this.customerMix = [];
    this.loyaltyBars = [];
    this.hasCustomerActivity = false;

    const data = this.insights;
    if (!data) return;

    this.hasCustomerActivity = data.activeCustomers > 0;

    // Zaman serisi gelir grafikleriyle aynı kırılımda — iki sekme arasında
    // geçerken okurun ölçeği yeniden öğrenmesi gerekmesin.
    const start = this.parseDayKey(this.reportStartDate);
    const end   = this.parseDayKey(this.reportEndDate);
    if (start && end && end >= start) {
      const byDay = new Map<string, { newCount: number; returningCount: number }>();
      for (const point of data.timeline ?? []) {
        byDay.set(this.dayKeyOf(point.date), {
          newCount: point.newCount, returningCount: point.returningCount,
        });
      }

      const rows: { date: Date; key: string; newCount: number; returningCount: number }[] = [];
      const cursor = new Date(start);
      while (cursor <= end && rows.length < 3700) {
        const key = this.toDayKey(cursor);
        const hit = byDay.get(key);
        rows.push({
          date: new Date(cursor), key,
          newCount: hit?.newCount ?? 0,
          returningCount: hit?.returningCount ?? 0,
        });
        cursor.setDate(cursor.getDate() + 1);
      }

      const bucket = this.bucketForRange(rows.length);
      const groups = new Map<string, { first: Date; last: Date; newCount: number; returningCount: number }>();
      for (const row of rows) {
        const key = this.bucketKey(row, bucket);
        const g = groups.get(key);
        if (g) {
          g.last = row.date;
          g.newCount += row.newCount;
          g.returningCount += row.returningCount;
        } else {
          groups.set(key, {
            first: row.date, last: row.date,
            newCount: row.newCount, returningCount: row.returningCount,
          });
        }
      }

      this.customerMix = [...groups.values()].map(g => ({
        ...this.bucketLabels(g.first, g.last, bucket),
        values: [g.returningCount, g.newCount],
      }));
    }

    // Sadakat kovaları sıralı: eksende soldan sağa bağlılık artıyor.
    this.loyaltyBars = [...(data.loyaltyBuckets ?? [])]
      .sort((a, b) => a.order - b.order)
      .map(b => ({
        label: b.label.replace(' ziyaret', '').replace('Hiç gelmemiş', 'Hiç'),
        fullLabel: b.label,
        value: b.count,
        note: data.totalCustomers > 0
          ? `müşterilerin %${Math.round(b.count * 100 / data.totalCustomers)}'i`
          : undefined,
      }));
  }

  /** Kayıp riski listesindeki bir müşteriye tek tıkla mesaj yazmak için. */
  whatsappLink(phone: string): string {
    const digits = (phone ?? '').replace(/\D/g, '');
    // 05xx… → 905xx…; zaten ülke koduyla başlıyorsa dokunulmuyor.
    const international = digits.startsWith('0') ? `9${digits}`
                        : digits.startsWith('90') ? digits
                        : digits.length === 10 ? `90${digits}` : digits;
    return `https://wa.me/${international}`;
  }

  formatDaysAgo(days: number): string {
    if (days < 1)   return 'bugün';
    if (days < 30)  return `${days} gün önce`;
    const months = Math.floor(days / 30);
    return months < 12 ? `${months} ay önce` : `${Math.floor(months / 12)} yıldan fazla`;
  }

  /** Haftanın başlangıcı pazartesi (TR takvimi). */
  private startOfWeek(date: Date): Date {
    const day = date.getDay() || 7;          // pazar = 7
    const monday = new Date(date);
    monday.setDate(date.getDate() - (day - 1));
    return monday;
  }

  /** API'den gelen "2026-01-05T00:00:00" → "2026-01-05". */
  private dayKeyOf(iso: string): string {
    return (iso ?? '').slice(0, 10);
  }

  private toDayKey(date: Date): string {
    const m = `${date.getMonth() + 1}`.padStart(2, '0');
    const d = `${date.getDate()}`.padStart(2, '0');
    return `${date.getFullYear()}-${m}-${d}`;
  }

  /** "2026-01-05" → yerel gece yarısı. new Date(str) UTC varsayıp gün kaydırıyor. */
  private parseDayKey(key: string): Date | null {
    const parts = (key ?? '').split('-').map(Number);
    if (parts.length !== 3 || parts.some(isNaN)) return null;
    return new Date(parts[0], parts[1] - 1, parts[2]);
  }

  // ── Yardımcılar ───────────────────────────────────────────────────────────

  private getDateString(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  formatCurrency(value: number, currency = 'TRY'): string {
    return new Intl.NumberFormat(this.langService.dateLocale, {
      style: 'currency',
      currency,
      minimumFractionDigits: 0
    }).format(value);
  }

  formatRate(currency: string, rate: number): string {
    return `1 ${currency} = ${new Intl.NumberFormat(this.langService.dateLocale, {
      style: 'currency', currency: 'TRY', minimumFractionDigits: 2
    }).format(rate)}`;
  }

  /**
   * Tarih iki biçimde gelebiliyor: yalnızca gün ("2026-10-04", gider kayıtları)
   * ya da tam zaman damgası ("2026-08-12T21:00:00Z", randevu zamanları).
   *
   * Eskiden ikisine de "T12:00:00" ekleniyordu; zaman damgası gelen yerlerde
   * "…T00:00:00T12:00:00" oluşup `Invalid Date` üretiyor ve sayfayı hataya
   * düşürüyordu (fiyat farkı penceresindeki tarihler bu yüzden boştu).
   */
  formatDate(dateStr: string): string {
    if (!dateStr) return '';

    const hasTime = dateStr.includes('T');
    const date = new Date(hasTime ? dateStr : `${dateStr}T12:00:00`);
    if (isNaN(date.getTime())) return '';

    return new Intl.DateTimeFormat(this.langService.dateLocale, {
      day: '2-digit', month: 'short', year: 'numeric',
      // Zaman damgası UTC geliyor; hangi güne denk geldiği Türkiye saatine
      // göre belirlenmeli. Yalnızca gün bilgisi gelen değerde saat dilimi
      // uygulamak, yurt dışındaki bir kullanıcıda günü kaydırırdı.
      ...(hasTime ? { timeZone: 'Europe/Istanbul' } : {}),
    }).format(date);
  }

  get todayStr(): string {
    return new Intl.DateTimeFormat(this.langService.dateLocale, {
      year: 'numeric', month: 'long', day: 'numeric'
    }).format(new Date());
  }

  setQuickRange(range: 'week' | 'month' | '30d' | 'year'): void {
    const now = new Date();
    let start: Date;
    switch (range) {
      case 'week': {
        const day = now.getDay() || 7;
        start = new Date(now); start.setDate(now.getDate() - day + 1); break;
      }
      case 'month':  start = new Date(now.getFullYear(), now.getMonth(), 1); break;
      case '30d':    start = new Date(now); start.setDate(now.getDate() - 30); break;
      case 'year':   start = new Date(now.getFullYear(), 0, 1); break;
    }
    this.reportStartDate = this.getDateString(start!);
    this.reportEndDate   = this.getDateString(now);
    this.loadEarnings();
    this.loadExpenses();
    if (this.insights) this.loadCustomerInsights();
  }

  onDateChange(): void {
    this.loadEarnings();
    this.loadExpenses();
    if (this.insights) this.loadCustomerInsights();
  }

  staffShare(earnings: number): number {
    if (!this.earnings?.totalInTry) return 0;
    return Math.round((earnings / this.earnings.totalInTry) * 100);
  }

  serviceShare(count: number): number {
    if (!this.earnings?.totalAppointments) return 0;
    return Math.round((count / this.earnings.totalAppointments) * 100);
  }

  expenseCategoryShare(total: number): number {
    if (!this.earnings?.totalExpenses) return 0;
    return Math.round((total / this.earnings.totalExpenses) * 100);
  }

  printReport(): void {
    window.print();
  }

  exportCSV(): void {
    if (!this.earnings) return;

    const lang = this.langService.lang();
    const e = this.earnings;
    const rows: string[][] = [];

    rows.push(['ayarlıyo — ' + (lang === 'tr' ? 'Kazanç Raporu' : lang === 'en' ? 'Earnings Report' : 'Отчёт о доходах')]);
    rows.push([lang === 'tr' ? 'Dönem' : lang === 'en' ? 'Period' : 'Период',
               `${this.reportStartDate} → ${this.reportEndDate}`]);
    rows.push([]);

    rows.push([lang === 'tr' ? 'ÖZET' : lang === 'en' ? 'SUMMARY' : 'СВОДКА']);
    rows.push([
      lang === 'tr' ? 'Toplam Gelir (TRY)' : lang === 'en' ? 'Total Income (TRY)' : 'Доход (TRY)',
      lang === 'tr' ? 'Toplam Gider (TRY)' : lang === 'en' ? 'Total Expenses (TRY)' : 'Расходы (TRY)',
      lang === 'tr' ? 'Net Kâr (TRY)' : lang === 'en' ? 'Net Profit (TRY)' : 'Чистая прибыль (TRY)',
      lang === 'tr' ? 'Toplam Randevu' : lang === 'en' ? 'Total Appointments' : 'Всего записей',
      lang === 'tr' ? 'Bu Ay (TRY)' : lang === 'en' ? 'This Month (TRY)' : 'Этот месяц (TRY)',
    ]);
    rows.push([
      e.totalInTry.toFixed(2),
      e.totalExpenses.toFixed(2),
      e.netProfit.toFixed(2),
      String(e.totalAppointments),
      e.monthEarnings.toFixed(2),
    ]);
    rows.push([]);

    rows.push([lang === 'tr' ? 'PERSONELE GÖRE KAZANÇ' : lang === 'en' ? 'EARNINGS BY STAFF' : 'ДОХОД ПО СОТРУДНИКАМ']);
    rows.push([
      lang === 'tr' ? 'Personel' : lang === 'en' ? 'Staff' : 'Сотрудник',
      lang === 'tr' ? 'Toplam Kazanç (TRY)' : lang === 'en' ? 'Total Earnings (TRY)' : 'Доход (TRY)',
      lang === 'tr' ? 'Randevu Sayısı' : lang === 'en' ? 'Appointments' : 'Записей',
      lang === 'tr' ? 'Ortalama (TRY)' : lang === 'en' ? 'Average (TRY)' : 'Среднее (TRY)',
    ]);
    for (const s of e.byStaff) {
      rows.push([s.staffName, s.totalEarnings.toFixed(2), String(s.appointmentCount), s.average.toFixed(2)]);
    }
    rows.push([]);

    rows.push([lang === 'tr' ? 'HİZMET BAZINDA KAZANÇ' : lang === 'en' ? 'EARNINGS BY SERVICE' : 'ДОХОД ПО УСЛУГАМ']);
    rows.push([
      lang === 'tr' ? 'Hizmet' : lang === 'en' ? 'Service' : 'Услуга',
      lang === 'tr' ? 'Para Birimi' : lang === 'en' ? 'Currency' : 'Валюта',
      lang === 'tr' ? 'Fiyat' : lang === 'en' ? 'Price' : 'Цена',
      lang === 'tr' ? 'Toplam Kazanç' : lang === 'en' ? 'Total Earnings' : 'Общий доход',
      lang === 'tr' ? 'Satış Sayısı' : lang === 'en' ? 'Sales Count' : 'Кол-во продаж',
    ]);
    for (const s of e.byService) {
      rows.push([s.serviceName, s.currency, s.price.toFixed(2), s.totalEarnings.toFixed(2), String(s.appointmentCount)]);
    }

    if (e.byCurrency.length > 1) {
      rows.push([]);
      rows.push([lang === 'tr' ? 'PARA BİRİMİNE GÖRE KAZANÇ' : lang === 'en' ? 'EARNINGS BY CURRENCY' : 'ДОХОД ПО ВАЛЮТАМ']);
      rows.push([
        lang === 'tr' ? 'Para Birimi' : lang === 'en' ? 'Currency' : 'Валюта',
        lang === 'tr' ? 'Toplam Kazanç' : lang === 'en' ? 'Total Earnings' : 'Общий доход',
        lang === 'tr' ? 'TRY Karşılığı' : lang === 'en' ? 'In TRY' : 'В TRY',
        lang === 'tr' ? 'Kur' : lang === 'en' ? 'Exchange Rate' : 'Курс',
        lang === 'tr' ? 'Randevu' : lang === 'en' ? 'Appointments' : 'Записей',
      ]);
      for (const c of e.byCurrency) {
        rows.push([c.currency, c.totalEarnings.toFixed(2), c.totalInTry.toFixed(2), c.exchangeRate.toFixed(4), String(c.appointmentCount)]);
      }
    }

    if (this.expenses.length > 0) {
      rows.push([]);
      rows.push([lang === 'tr' ? 'GİDERLER' : lang === 'en' ? 'EXPENSES' : 'РАСХОДЫ']);
      rows.push([
        lang === 'tr' ? 'Tarih' : lang === 'en' ? 'Date' : 'Дата',
        lang === 'tr' ? 'Kategori' : lang === 'en' ? 'Category' : 'Категория',
        lang === 'tr' ? 'Tutar' : lang === 'en' ? 'Amount' : 'Сумма',
        lang === 'tr' ? 'Para Birimi' : lang === 'en' ? 'Currency' : 'Валюта',
        lang === 'tr' ? 'Açıklama' : lang === 'en' ? 'Description' : 'Описание',
      ]);
      for (const exp of this.expenses) {
        rows.push([exp.date, exp.category, exp.amount.toFixed(2), exp.currency, exp.description ?? '']);
      }
    }

    const escape = (v: string) => `"${(v ?? '').toString().replace(/"/g, '""')}"`;
    // Türkçe Excel: ayırıcı olarak noktalı virgül (;) kullanılır, virgül (,) değil.
    // sep= satırı BOM tespitini bozduğu için kaldırıldı.
    // UTF-8 BOM (0xEF 0xBB 0xBF) Excel'e "bu dosya UTF-8" bilgisini verir.
    const csvBody    = rows.map(r => r.map(escape).join(';')).join('\r\n');
    const bom        = new Uint8Array([0xEF, 0xBB, 0xBF]);
    const encoded    = new TextEncoder().encode(csvBody);
    const blob       = new Blob([bom, encoded], { type: 'text/csv;charset=utf-8;' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `ayarliyo-rapor-${this.reportStartDate}-${this.reportEndDate}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
