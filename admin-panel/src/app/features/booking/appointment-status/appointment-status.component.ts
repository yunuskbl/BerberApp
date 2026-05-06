import { Component, ElementRef, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { BookingApiService } from '../../../core/services/booking.service';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-appointment-status',
  standalone: true,
  imports: [CommonModule, TranslatePipe],
  templateUrl: './appointment-status.component.html',
  styleUrl: './appointment-status.component.scss',
})
export class AppointmentStatusComponent implements OnInit {
  appointment: any = null;
  salon: any = null;
  isLoading = true;
  errorMessage = '';
  subdomain = '';
  copied = false;

  isCancelling = false;
  cancelError = '';
  cancelSuccess = false;
  showCancelConfirm = false;
  private phone = '';
  private appointmentId = '';

  constructor(
    private route: ActivatedRoute,
    private bookingService: BookingApiService,
    private el: ElementRef,
    public langService: LanguageService,
  ) {}

  ngOnInit(): void {
    const subdomain = this.route.snapshot.paramMap.get('subdomain') || '';
    const appointmentId = this.route.snapshot.paramMap.get('appointmentId') || '';
    const phone = this.route.snapshot.queryParamMap.get('phone') || '';
    this.subdomain = subdomain;
    this.appointmentId = appointmentId;
    this.phone = phone;
    this.loadSalon(subdomain);
    this.loadStatus(subdomain, appointmentId, phone);
  }

  loadSalon(subdomain: string): void {
    this.bookingService.getSalon(subdomain).subscribe({
      next: (res) => {
        if (res.success && res.data) {
          this.salon = res.data;
          this.applyTheme(res.data.themeColor || '#7c3aed');
        }
      },
    });
  }

  private applyTheme(color: string): void {
    const el = this.el.nativeElement as HTMLElement;
    el.style.setProperty('--theme', color);
    el.style.setProperty('--theme-dark', this.darkenColor(color, 0.28));
    el.style.setProperty('--theme-light', this.lightenColor(color, 0.88));
  }

  private darkenColor(hex: string, factor: number): string {
    hex = hex.replace('#', '');
    if (hex.length === 3) hex = hex.split('').map(c => c + c).join('');
    const num = parseInt(hex, 16);
    const r = Math.round(((num >> 16) & 0xff) * (1 - factor));
    const g = Math.round(((num >> 8) & 0xff) * (1 - factor));
    const b = Math.round((num & 0xff) * (1 - factor));
    return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
  }

  private lightenColor(hex: string, factor: number): string {
    hex = hex.replace('#', '');
    if (hex.length === 3) hex = hex.split('').map(c => c + c).join('');
    const num = parseInt(hex, 16);
    const r = Math.round(((num >> 16) & 0xff) + (255 - ((num >> 16) & 0xff)) * factor);
    const g = Math.round(((num >> 8) & 0xff) + (255 - ((num >> 8) & 0xff)) * factor);
    const b = Math.round((num & 0xff) + (255 - (num & 0xff)) * factor);
    return `#${r.toString(16).padStart(2, '0')}${g.toString(16).padStart(2, '0')}${b.toString(16).padStart(2, '0')}`;
  }

  loadStatus(subdomain: string, appointmentId: string, phone: string): void {
    this.bookingService.getAppointmentStatus(subdomain, appointmentId, phone).subscribe({
      next: (res) => {
        if (res.success) this.appointment = res.data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = this.langService.t('apptStatus.notFoundMsg');
        this.isLoading = false;
      },
    });
  }

  get statusUrl(): string {
    return window.location.href;
  }

  copyLink(): void {
    navigator.clipboard.writeText(this.statusUrl).then(() => {
      this.copied = true;
      setTimeout(() => (this.copied = false), 2500);
    });
  }

  shareOnWhatsApp(): void {
    const text = `${this.langService.t('apptStatus.whatsappShare')}${this.statusUrl}`;
    window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, '_blank');
  }

  confirmCancel(): void {
    this.showCancelConfirm = true;
    this.cancelError = '';
  }

  dismissCancel(): void {
    this.showCancelConfirm = false;
  }

  cancelAppointment(): void {
    this.isCancelling = true;
    this.cancelError = '';
    this.showCancelConfirm = false;

    this.bookingService.cancelAppointment(this.subdomain, this.appointmentId, this.phone).subscribe({
      next: (res) => {
        if (res.success) {
          this.cancelSuccess = true;
          this.appointment = { ...this.appointment, status: 'Cancelled' };
        }
        this.isCancelling = false;
      },
      error: (err) => {
        this.cancelError = err.error?.message || 'Randevu iptal edilemedi.';
        this.isCancelling = false;
      },
    });
  }

  get canCustomerCancel(): boolean {
    return this.appointment?.status === 'Confirmed' && !!this.phone;
  }

  getStatusIcon(): string {
    switch (this.appointment?.status) {
      case 'Pending':   return '⏳';
      case 'Confirmed': return '✓';
      case 'Cancelled': return '✕';
      case 'Completed': return '★';
      default:          return '?';
    }
  }

  getStatusText(): string {
    switch (this.appointment?.status) {
      case 'Pending':   return this.langService.t('apptStatus.pending.text');
      case 'Confirmed': return this.langService.t('apptStatus.confirmed.text');
      case 'Cancelled': return this.langService.t('apptStatus.cancelled.text');
      case 'Completed': return this.langService.t('apptStatus.completed.text');
      default:          return this.langService.t('apptStatus.unknown');
    }
  }

  getStatusSubtext(): string {
    switch (this.appointment?.status) {
      case 'Pending':   return this.langService.t('apptStatus.pending.sub');
      case 'Confirmed': return this.langService.t('apptStatus.confirmed.sub');
      case 'Cancelled': return this.langService.t('apptStatus.cancelled.sub');
      case 'Completed': return this.langService.t('apptStatus.completed.sub');
      default:          return '';
    }
  }

  getStatusClass(): string {
    switch (this.appointment?.status) {
      case 'Pending':   return 'status-pending';
      case 'Confirmed': return 'status-confirmed';
      case 'Cancelled': return 'status-cancelled';
      case 'Completed': return 'status-completed';
      default:          return '';
    }
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString(this.langService.dateLocale, {
      weekday: 'long', day: 'numeric', month: 'long', year: 'numeric',
      timeZone: 'Europe/Istanbul',
    });
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString(this.langService.dateLocale, {
      hour: '2-digit', minute: '2-digit',
      timeZone: 'Europe/Istanbul',
    });
  }

  formatPrice(price: number, currency: string): string {
    return new Intl.NumberFormat(this.langService.dateLocale, {
      style: 'currency', currency: currency || 'TRY',
    }).format(price);
  }
}
