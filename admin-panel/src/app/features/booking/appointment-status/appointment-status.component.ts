import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { BookingApiService } from '../../../core/services/booking.service';

@Component({
  selector: 'app-appointment-status',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './appointment-status.component.html',
  styleUrl: './appointment-status.component.scss',
})
export class AppointmentStatusComponent implements OnInit {
  appointment: any = null;
  isLoading = true;
  errorMessage = '';
  subdomain = '';
  copied = false;

  constructor(
    private route: ActivatedRoute,
    private bookingService: BookingApiService,
  ) {}

  ngOnInit(): void {
    const subdomain = this.route.snapshot.paramMap.get('subdomain') || '';
    const appointmentId = this.route.snapshot.paramMap.get('appointmentId') || '';
    this.subdomain = subdomain;
    this.loadStatus(subdomain, appointmentId);
  }

  loadStatus(subdomain: string, appointmentId: string): void {
    this.bookingService.getAppointmentStatus(subdomain, appointmentId).subscribe({
      next: (res) => {
        if (res.success) this.appointment = res.data;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Randevu bulunamadı.';
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
    const text = `Randevu takip linkim: ${this.statusUrl}`;
    window.open(`https://wa.me/?text=${encodeURIComponent(text)}`, '_blank');
  }

  getStatusIcon(): string {
    switch (this.appointment?.status) {
      case 'Pending': return '⏳';
      case 'Confirmed': return '✅';
      case 'Cancelled': return '❌';
      case 'Completed': return '🎉';
      default: return '❓';
    }
  }

  getStatusText(): string {
    switch (this.appointment?.status) {
      case 'Pending': return 'Onay Bekleniyor';
      case 'Confirmed': return 'Onaylandı';
      case 'Cancelled': return 'İptal Edildi';
      case 'Completed': return 'Tamamlandı';
      default: return 'Bilinmiyor';
    }
  }

  getStatusClass(): string {
    switch (this.appointment?.status) {
      case 'Pending': return 'status-pending';
      case 'Confirmed': return 'status-confirmed';
      case 'Cancelled': return 'status-cancelled';
      case 'Completed': return 'status-completed';
      default: return '';
    }
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR', {
      weekday: 'long', day: 'numeric', month: 'long'
    });
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit', minute: '2-digit'
    });
  }

  formatPrice(price: number, currency: string): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency', currency: currency || 'TRY'
    }).format(price);
  }
}
