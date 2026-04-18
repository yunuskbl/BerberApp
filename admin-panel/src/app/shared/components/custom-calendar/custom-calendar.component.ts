import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-custom-calendar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-calendar.component.html',
  styleUrl: './custom-calendar.component.scss'
})
export class CustomCalendarComponent implements OnInit {
  @Input() minDate: string = '';
  @Input() maxDate: string = '';
  @Input() value:   string = '';
  @Output() valueChange = new EventEmitter<string>();

  currentYear:  number = 0;
  currentMonth: number = 0;
  days:         (number | null)[] = [];

  weekDays = ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'];
  months   = [
    'Ocak','Şubat','Mart','Nisan','Mayıs','Haziran',
    'Temmuz','Ağustos','Eylül','Ekim','Kasım','Aralık'
  ];

  ngOnInit(): void {
    const today      = new Date();
    this.currentYear  = today.getFullYear();
    this.currentMonth = today.getMonth();
    this.buildCalendar();
  }

  buildCalendar(): void {
    const firstDay = new Date(this.currentYear, this.currentMonth, 1);
    const lastDay  = new Date(this.currentYear, this.currentMonth + 1, 0);

    // Pazartesi başlangıç için ayarlama
    let startOffset = firstDay.getDay() - 1;
    if (startOffset < 0) startOffset = 6;

    this.days = [];
    for (let i = 0; i < startOffset; i++) this.days.push(null);
    for (let i = 1; i <= lastDay.getDate(); i++) this.days.push(i);
  }

  prevMonth(): void {
    if (this.currentMonth === 0) {
      this.currentMonth = 11;
      this.currentYear--;
    } else {
      this.currentMonth--;
    }
    this.buildCalendar();
  }

  nextMonth(): void {
    if (this.currentMonth === 11) {
      this.currentMonth = 0;
      this.currentYear++;
    } else {
      this.currentMonth++;
    }
    this.buildCalendar();
  }

  selectDay(day: number | null): void {
    if (!day || this.isDisabled(day)) return;
    const month = String(this.currentMonth + 1).padStart(2, '0');
    const d     = String(day).padStart(2, '0');
    const value = `${this.currentYear}-${month}-${d}`;
    this.value  = value;
    this.valueChange.emit(value);
  }

  isSelected(day: number | null): boolean {
    if (!day) return false;
    const month = String(this.currentMonth + 1).padStart(2, '0');
    const d     = String(day).padStart(2, '0');
    return this.value === `${this.currentYear}-${month}-${d}`;
  }

  isToday(day: number | null): boolean {
    if (!day) return false;
    const today = new Date();
    return day === today.getDate() &&
           this.currentMonth === today.getMonth() &&
           this.currentYear  === today.getFullYear();
  }

  isDisabled(day: number | null): boolean {
    if (!day) return true;
    const month = String(this.currentMonth + 1).padStart(2, '0');
    const d     = String(day).padStart(2, '0');
    const date  = `${this.currentYear}-${month}-${d}`;
    if (this.minDate && date < this.minDate) return true;
    if (this.maxDate && date > this.maxDate) return true;
    return false;
  }

  canGoPrev(): boolean {
    const min = this.minDate ? new Date(this.minDate) : null;
    if (!min) return true;
    return this.currentYear > min.getFullYear() ||
           (this.currentYear === min.getFullYear() && this.currentMonth > min.getMonth());
  }

  canGoNext(): boolean {
    const max = this.maxDate ? new Date(this.maxDate) : null;
    if (!max) return true;
    return this.currentYear < max.getFullYear() ||
           (this.currentYear === max.getFullYear() && this.currentMonth < max.getMonth());
  }

  get selectedDisplayDate(): string {
    if (!this.value) return '';
    const d = new Date(this.value);
    return d.toLocaleDateString('tr-TR', {
      day: 'numeric', month: 'long', year: 'numeric'
    });
  }
}