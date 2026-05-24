import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { EarningsService, EarningsDto } from '../../../core/services/earnings.service';
import { ExpenseService, ExpenseDto } from '../../../core/services/expense.service';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './reports-list.component.html',
  styleUrl: './reports-list.component.scss'
})
export class ReportsListComponent implements OnInit {
  earnings: EarningsDto | null = null;
  isLoading = false;

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
  ) {}

  ngOnInit(): void {
    this.loadEarnings();
    this.loadExpenses();
    this.initExpenseForm();
  }

  // ── Gelir ─────────────────────────────────────────────────────────────────

  loadEarnings(): void {
    this.isLoading = true;
    this.earningsService.getEarnings(this.reportStartDate, this.reportEndDate).subscribe({
      next: (res) => {
        if (res.success) this.earnings = res.data;
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
