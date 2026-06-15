import { Component, Input, Output, EventEmitter, OnInit, OnChanges, SimpleChanges, ElementRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { LanguageService } from '../../../core/services/language.service';

@Component({
  selector: 'app-custom-calendar',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './custom-calendar.component.html',
  styleUrl: './custom-calendar.component.scss'
})
export class CustomCalendarComponent implements OnInit, OnChanges {
  @Input() mode:        'single' | 'range' = 'single';
  @Input() minDate:     string   = '';
  @Input() maxDate:     string   = '';
  @Input() value:       string   = '';
  @Input() markedDates: string[] = [];
  @Input() startDate:   string   = '';
  @Input() endDate:     string   = '';
  @Input() themeColor:  string   = '#7c3aed';
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

  constructor(public langService: LanguageService, private el: ElementRef) {}

  ngOnInit(): void {
    this.applyTheme(this.themeColor);
    const today      = new Date();
    this.currentYear  = today.getFullYear();
    this.currentMonth = today.getMonth();
    this.buildCalendar();
  }

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['themeColor']) {
      this.applyTheme(this.themeColor);
    }
  }

  private applyTheme(hex: string): void {
    if (!hex) return;
    const host = this.el.nativeElement as HTMLElement;
    const dark       = this.darkenHex(hex, 0.32);
    const light      = this.lightenHex(hex, 0.90);
    const lightText  = this.darkenHex(hex, 0.08);
    const rgb        = this.hexToRgb(hex);
    host.style.setProperty('--cal-primary',    hex);
    host.style.setProperty('--cal-dark',        dark);
    host.style.setProperty('--cal-light',       light);
    host.style.setProperty('--cal-light-text',  lightText);
    if (rgb) {
      host.style.setProperty('--cal-shadow',    `rgba(${rgb.r},${rgb.g},${rgb.b},0.35)`);
      host.style.setProperty('--cal-shadow-sm', `rgba(${rgb.r},${rgb.g},${rgb.b},0.20)`);
    }
  }

  private hexToRgb(hex: string): { r: number; g: number; b: number } | null {
    const result = /^#?([a-f\d]{2})([a-f\d]{2})([a-f\d]{2})$/i.exec(hex.trim());
    return result ? {
      r: parseInt(result[1], 16),
      g: parseInt(result[2], 16),
      b: parseInt(result[3], 16),
    } : null;
  }

  private darkenHex(hex: string, amount: number): string {
    const rgb = this.hexToRgb(hex);
    if (!rgb) return hex;
    const r = Math.max(0, Math.round(rgb.r * (1 - amount)));
    const g = Math.max(0, Math.round(rgb.g * (1 - amount)));
    const b = Math.max(0, Math.round(rgb.b * (1 - amount)));
    return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
  }

  private lightenHex(hex: string, amount: number): string {
    const rgb = this.hexToRgb(hex);
    if (!rgb) return hex;
    const r = Math.min(255, Math.round(rgb.r + (255 - rgb.r) * amount));
    const g = Math.min(255, Math.round(rgb.g + (255 - rgb.g) * amount));
    const b = Math.min(255, Math.round(rgb.b + (255 - rgb.b) * amount));
    return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
  }

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
    return { tr: 'Seçilen', en: 'Selected', ru: 'Выбрано', de: 'Ausgewählt' }[this.langService.lang()] ?? 'Seçilen';
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
