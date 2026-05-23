import { Component, Input, Output, EventEmitter, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-custom-calendar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-calendar.component.html',
  styleUrl: './custom-calendar.component.scss'
})
export class CustomCalendarComponent implements OnInit {
  @Input() mode:        'single' | 'range' = 'single';
  @Input() minDate:     string   = '';
  @Input() maxDate:     string   = '';
  @Input() value:       string   = '';
  @Input() markedDates: string[] = [];
  @Input() startDate:   string   = '';
  @Input() endDate:     string   = '';
  @Output() valueChange      = new EventEmitter<string>();
  @Output() startDateChange  = new EventEmitter<string>();
  @Output() endDateChange    = new EventEmitter<string>();

  hoverDate: string = '';

  currentYear:  number = 0;
  currentMonth: number = 0;
  days:         (number | null)[] = [];

  private static readonly WEEK_DAYS: Record<string, string[]> = {
    tr: ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'],
    en: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
    ru: ['Пн',  'Вт',  'Ср',  'Чт',  'Пт',  'Сб',  'Вс' ],
  };

  constructor(public langService: LanguageService) {}

  get weekDays(): string[] {
    return CustomCalendarComponent.WEEK_DAYS[this.langService.lang()] ?? CustomCalendarComponent.WEEK_DAYS['tr'];
  }

  get months(): string[] {
    const locale = this.langService.dateLocale;
    return Array.from({ length: 12 }, (_, i) =>
      new Intl.DateTimeFormat(locale, { month: 'long' }).format(new Date(2000, i, 1))
    );
  }

  get selectedLabel(): string {
    return { tr: 'Seçilen', en: 'Selected', ru: 'Выбрано' }[this.langService.lang()] ?? 'Seçilen';
  }

  ngOnInit(): void {
    const today      = new Date();
    this.currentYear  = today.getFullYear();
    this.currentMonth = today.getMonth();
    this.buildCalendar();
  }

  buildCalendar(): void {
    const firstDay = new Date(this.currentYear, this.currentMonth, 1);
    const lastDay  = new Date(this.currentYear, this.currentMonth + 1, 0);

    let startOffset = firstDay.getDay() - 1;
    if (startOffset < 0) startOffset = 6;

    this.days = [];
    for (let i = 0; i < startOffset; i++) this.days.push(null);
    for (let i = 1; i <= lastDay.getDate(); i++) this.days.push(i);
  }

  prevMonth(): void {
    if (this.currentMonth === 0) { this.currentMonth = 11; this.currentYear--; }
    else this.currentMonth--;
    this.buildCalendar();
  }

  nextMonth(): void {
    if (this.currentMonth === 11) { this.currentMonth = 0; this.currentYear++; }
    else this.currentMonth++;
    this.buildCalendar();
  }

  buildDateStr(day: number): string {
    const month = String(this.currentMonth + 1).padStart(2, '0');
    const d     = String(day).padStart(2, '0');
    return `${this.currentYear}-${month}-${d}`;
  }

  selectDay(day: number | null): void {
    if (!day || this.isDisabled(day)) return;
    const date = this.buildDateStr(day);

    if (this.mode === 'single') {
      this.value = date;
      this.valueChange.emit(date);
      return;
    }

    // Range mode
    if (!this.startDate || (this.startDate && this.endDate)) {
      this.startDate = date;
      this.endDate   = '';
      this.startDateChange.emit(date);
      this.endDateChange.emit('');
    } else {
      if (date >= this.startDate) {
        this.endDate = date;
        this.hoverDate = '';
        this.endDateChange.emit(date);
      } else {
        this.startDate = date;
        this.endDate   = '';
        this.startDateChange.emit(date);
        this.endDateChange.emit('');
      }
    }
  }

  onDayHover(day: number | null): void {
    if (this.mode !== 'range' || !this.startDate || this.endDate || !day) {
      this.hoverDate = '';
      return;
    }
    this.hoverDate = this.buildDateStr(day);
  }

  isSelected(day: number | null): boolean {
    if (!day || this.mode !== 'single') return false;
    return this.value === this.buildDateStr(day);
  }

  isRangeStart(day: number | null): boolean {
    if (!day || this.mode !== 'range') return false;
    return this.startDate === this.buildDateStr(day);
  }

  isRangeEnd(day: number | null): boolean {
    if (!day || this.mode !== 'range') return false;
    return !!this.endDate && this.endDate === this.buildDateStr(day);
  }

  isInRange(day: number | null): boolean {
    if (!day || this.mode !== 'range' || !this.startDate) return false;
    const date = this.buildDateStr(day);
    const end  = this.endDate || this.hoverDate;
    if (!end || end < this.startDate) return false;
    return date > this.startDate && date < end;
  }

  isToday(day: number | null): boolean {
    if (!day) return false;
    const today = new Date();
    return day === today.getDate() &&
           this.currentMonth === today.getMonth() &&
           this.currentYear  === today.getFullYear();
  }

  isMarked(day: number | null): boolean {
    if (!day) return false;
    return this.markedDates.includes(this.buildDateStr(day));
  }

  isDisabled(day: number | null): boolean {
    if (!day) return true;
    const date = this.buildDateStr(day);
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

  get rangeDisplayDate(): string {
    if (!this.startDate) return '';
    const locale = this.langService.dateLocale;
    const opts: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' };
    const start = new Date(this.startDate + 'T12:00:00');
    if (!this.endDate || this.startDate === this.endDate) {
      return start.toLocaleDateString(locale, { ...opts, year: 'numeric' });
    }
    const end = new Date(this.endDate + 'T12:00:00');
    return `${start.toLocaleDateString(locale, opts)} – ${end.toLocaleDateString(locale, { ...opts, year: 'numeric' })}`;
  }

  get selectedDisplayDate(): string {
    if (!this.value) return '';
    const d = new Date(this.value + 'T12:00:00');
    return d.toLocaleDateString(this.langService.dateLocale, {
      day: 'numeric', month: 'long', year: 'numeric'
    });
  }
}
