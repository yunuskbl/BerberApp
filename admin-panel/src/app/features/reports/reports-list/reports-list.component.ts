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

  formatCurrency(value: number): string {
    return new Intl.NumberFormat(this.langService.dateLocale, {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 0
    }).format(value);
  }
}
