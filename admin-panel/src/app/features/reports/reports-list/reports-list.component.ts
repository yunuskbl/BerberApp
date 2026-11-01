import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { EarningsService, EarningsDto, PriceDifferenceDetailDto } from '../../../core/services/earnings.service';
import { ExpenseService, ExpenseDto } from '../../../core/services/expense.service';
import { LanguageService } from '../../../core/services/language.service';
import { AuthService } from '../../../core/services/auth.service';
import { BranchService } from '../../../core/services/branch.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { TrendChartComponent } from '../../../shared/components/charts/trend-chart.component';
import { BarChartComponent } from '../../../shared/components/charts/bar-chart.component';
import { CHART_INK, ChartPoint, compactNumber } from '../../../shared/components/charts/chart.model';
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
    TrendChartComponent, BarChartComponent,
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
    const levels: Record<string, number> = { Baslangic: 1, Profesyonel: 2, Premium: 3 };
    return (levels[this.authService.getUserPlan()] ?? 1) >= 3;
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
  activeTab: 'income' | 'expense' = 'income';

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

  // Grafik bileşenlerine girdi olarak geçen biçimleyiciler. Ok işlevi olduğu
  // için `this` bağlamı şablonda da korunuyor.
  readonly moneyFull = (v: number): string => this.formatCurrency(v);
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

    const days = this.buildDayRows(e.daily ?? [], e.expenseDaily ?? []);
    if (days.length === 0) return;

    this.hasChartData = days.some(d => d.income > 0 || d.count > 0 || d.expense > 0);
    if (!this.hasChartData) return;

    this.chartBucket = days.length <= 45 ? 'day' : days.length <= 182 ? 'week' : 'month';
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

  private bucketDays(days: DayRow[], bucket: 'day' | 'week' | 'month') {
    const locale = this.langService.dateLocale;
    const shortDay   = new Intl.DateTimeFormat(locale, { day: 'numeric', month: 'short' });
    const longDay    = new Intl.DateTimeFormat(locale, { weekday: 'short', day: 'numeric', month: 'long' });
    const shortMonth = new Intl.DateTimeFormat(locale, { month: 'short' });
    const longMonth  = new Intl.DateTimeFormat(locale, { month: 'long', year: 'numeric' });

    const groups = new Map<string, { first: Date; last: Date; income: number; count: number; expense: number }>();
    for (const d of days) {
      const key = bucket === 'day'   ? d.key
                : bucket === 'week'  ? this.toDayKey(this.startOfWeek(d.date))
                :                      d.key.slice(0, 7);
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
      label: bucket === 'month' ? shortMonth.format(g.first) : shortDay.format(g.first),
      fullLabel:
        bucket === 'day'   ? longDay.format(g.first)
      : bucket === 'week'  ? `${shortDay.format(g.first)} – ${shortDay.format(g.last)}`
      :                      longMonth.format(g.first),
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

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    return new Intl.DateTimeFormat(this.langService.dateLocale, {
      day: '2-digit', month: 'short', year: 'numeric'
    }).format(new Date(dateStr + 'T12:00:00'));
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
  }

  onDateChange(): void {
    this.loadEarnings();
    this.loadExpenses();
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
