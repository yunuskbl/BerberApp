import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { EarningsService, EarningsDto } from '../../../core/services/earnings.service';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-reports-list',
  standalone: true,
  imports: [CommonModule, FormsModule, TranslatePipe],
  templateUrl: './reports-list.component.html',
  styleUrl: './reports-list.component.scss'
})
export class ReportsListComponent implements OnInit {
  earnings: EarningsDto | null = null;
  isLoading = false;

  isStartDateOpen = false;
  isEndDateOpen = false;

  reportStartDate = this.getDateString(new Date(new Date().setDate(new Date().getDate() - 30)));
  reportEndDate = this.getDateString(new Date());

  constructor(
    private earningsService: EarningsService,
    public langService: LanguageService,
  ) {}

  ngOnInit(): void {
    this.loadEarnings();
  }

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
  }

  staffShare(earnings: number): number {
    if (!this.earnings?.totalInTry) return 0;
    return Math.round((earnings / this.earnings.totalInTry) * 100);
  }

  serviceShare(count: number): number {
    if (!this.earnings?.totalAppointments) return 0;
    return Math.round((count / this.earnings.totalAppointments) * 100);
  }

  printReport(): void {
    window.print();
  }

  exportCSV(): void {
    if (!this.earnings) return;

    const lang = this.langService.lang();
    const e = this.earnings;
    const rows: string[][] = [];

    // ── Header info ──
    rows.push(['ayarlıyo — ' + (lang === 'tr' ? 'Kazanç Raporu' : lang === 'en' ? 'Earnings Report' : 'Отчёт о доходах')]);
    rows.push([lang === 'tr' ? 'Dönem' : lang === 'en' ? 'Period' : 'Период',
               `${this.reportStartDate} → ${this.reportEndDate}`]);
    rows.push([]);

    // ── Summary ──
    rows.push([lang === 'tr' ? 'ÖZET' : lang === 'en' ? 'SUMMARY' : 'СВОДКА']);
    rows.push([
      lang === 'tr' ? 'Toplam Kazanç (TRY)' : lang === 'en' ? 'Total Earnings (TRY)' : 'Общий доход (TRY)',
      lang === 'tr' ? 'Toplam Randevu' : lang === 'en' ? 'Total Appointments' : 'Всего записей',
      lang === 'tr' ? 'Ort. Randevu Başına' : lang === 'en' ? 'Avg. Per Appt' : 'Сред. за запись',
      lang === 'tr' ? 'Bu Ay (TRY)' : lang === 'en' ? 'This Month (TRY)' : 'Этот месяц (TRY)',
      lang === 'tr' ? 'Bu Hafta (TRY)' : lang === 'en' ? 'This Week (TRY)' : 'Эта неделя (TRY)',
    ]);
    rows.push([
      e.totalInTry.toFixed(2),
      String(e.totalAppointments),
      e.averagePerAppointment.toFixed(2),
      e.monthEarnings.toFixed(2),
      e.weekEarnings.toFixed(2),
    ]);
    rows.push([]);

    // ── By Staff ──
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

    // ── By Service ──
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

    // ── By Currency (if multi-currency) ──
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

    // ── Build CSV string ──
    // UTF-8 BOM + sep=, directive → forces Excel to use comma regardless of locale
    const escape = (v: string) => `"${v.replace(/"/g, '""')}"`;
    const csvBody = rows.map(r => r.map(escape).join(',')).join('\r\n');
    const csv = '﻿' + 'sep=,\r\n' + csvBody;

    // ── Download ──
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' });
    const url  = URL.createObjectURL(blob);
    const a    = document.createElement('a');
    a.href     = url;
    a.download = `ayarliyo-rapor-${this.reportStartDate}-${this.reportEndDate}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  }
}
