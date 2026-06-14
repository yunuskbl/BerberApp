import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterModule } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BookingApiService } from '../../core/services/booking.service';

interface Appointment {
  id: string;
  startTime: string;
  status: string;
  serviceName: string;
  staffName: string;
}

@Component({
  selector: 'app-my-appointments',
  standalone: true,
  imports: [CommonModule, RouterModule, FormsModule],
  template: `
    <div class="page-wrap">
      <div class="card">
        <div class="brand">✂️ ayarlıyo</div>
        <h1 class="title">Randevularım</h1>
        @if (salonName) {
          <p class="salon-name">{{ salonName }}</p>
        }

        @if (!phoneSubmitted) {
          <div class="phone-form">
            <p class="hint">Randevularınızı görmek için telefon numaranızı girin.</p>
            <input
              class="input"
              type="tel"
              [(ngModel)]="phoneInput"
              placeholder="05XX XXX XX XX"
              maxlength="15"
              (keyup.enter)="search()" />
            <button class="btn-primary" [disabled]="isLoading" (click)="search()">
              {{ isLoading ? 'Yükleniyor...' : 'Randevuları Gör' }}
            </button>
          </div>
        }

        @if (phoneSubmitted) {
          @if (isLoading) {
            <p class="hint">Yükleniyor...</p>
          } @else if (appointments.length === 0) {
            <div class="empty">
              <span class="empty-icon">📅</span>
              <p>Bu salonda henüz randevunuz bulunmuyor.</p>
              <a class="btn-secondary" [routerLink]="['/book', subdomain]">Randevu Al</a>
            </div>
          } @else {
            <ul class="list">
              @for (apt of appointments; track apt.id) {
                <li class="item">
                  <div class="item-header">
                    <span class="date">{{ apt.startTime | date:'d MMMM yyyy, HH:mm':'':'tr' }}</span>
                    <span class="badge" [class]="'badge-' + apt.status.toLowerCase()">
                      {{ statusLabel(apt.status) }}
                    </span>
                  </div>
                  <div class="item-detail">💈 {{ apt.serviceName }}</div>
                  @if (apt.staffName) {
                    <div class="item-detail">👤 {{ apt.staffName }}</div>
                  }
                </li>
              }
            </ul>
            <a class="btn-secondary" [routerLink]="['/book', subdomain]">Yeni Randevu Al</a>
          }
        }
      </div>
    </div>
  `,
  styles: [`
    .page-wrap { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: #f5f5f5; padding: 16px; }
    .card { background: #fff; border-radius: 16px; padding: 32px 24px; max-width: 480px; width: 100%; box-shadow: 0 4px 24px rgba(0,0,0,.08); }
    .brand { font-size: 13px; color: #888; margin-bottom: 8px; }
    .title { font-size: 22px; font-weight: 700; margin: 0 0 4px; }
    .salon-name { color: #555; font-size: 14px; margin: 0 0 24px; }
    .hint { color: #666; font-size: 14px; margin-bottom: 16px; }
    .phone-form { display: flex; flex-direction: column; gap: 12px; }
    .input { border: 1.5px solid #ddd; border-radius: 10px; padding: 12px 14px; font-size: 15px; outline: none; }
    .input:focus { border-color: #111; }
    .btn-primary { background: #111; color: #fff; border: none; border-radius: 10px; padding: 13px; font-size: 15px; font-weight: 600; cursor: pointer; }
    .btn-primary:disabled { opacity: .5; }
    .btn-secondary { display: block; text-align: center; margin-top: 20px; background: #f0f0f0; color: #111; border-radius: 10px; padding: 12px; font-size: 14px; font-weight: 600; text-decoration: none; }
    .empty { text-align: center; padding: 24px 0; color: #555; }
    .empty-icon { font-size: 40px; display: block; margin-bottom: 12px; }
    .list { list-style: none; padding: 0; margin: 0; display: flex; flex-direction: column; gap: 12px; }
    .item { border: 1.5px solid #eee; border-radius: 12px; padding: 14px 16px; }
    .item-header { display: flex; justify-content: space-between; align-items: center; margin-bottom: 8px; }
    .date { font-weight: 600; font-size: 14px; }
    .item-detail { font-size: 13px; color: #555; margin-top: 4px; }
    .badge { font-size: 11px; font-weight: 600; padding: 3px 10px; border-radius: 20px; }
    .badge-pending   { background: #fff3cd; color: #856404; }
    .badge-confirmed { background: #d1e7dd; color: #0f5132; }
    .badge-completed { background: #cfe2ff; color: #084298; }
    .badge-cancelled { background: #f8d7da; color: #842029; }
    .badge-noshow    { background: #e2e3e5; color: #41464b; }
  `]
})
export class MyAppointmentsComponent implements OnInit {
  subdomain = '';
  phoneInput = '';
  phoneSubmitted = false;
  isLoading = false;
  appointments: Appointment[] = [];
  salonName = '';

  constructor(
    private route: ActivatedRoute,
    private bookingService: BookingApiService,
  ) {}

  ngOnInit(): void {
    this.subdomain = this.route.snapshot.paramMap.get('subdomain') ?? '';
    const phone = this.route.snapshot.queryParamMap.get('phone');
    if (phone) {
      this.phoneInput = phone;
      this.search();
    }
  }

  search(): void {
    if (!this.phoneInput.trim()) return;
    this.isLoading = true;
    this.phoneSubmitted = true;
    this.bookingService.getMyAppointments(this.subdomain, this.phoneInput.trim()).subscribe({
      next: (res: any) => {
        this.appointments = res.data ?? [];
        this.salonName = res.salonName ?? '';
        this.isLoading = false;
      },
      error: () => {
        this.appointments = [];
        this.isLoading = false;
      }
    });
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Pending: 'Beklemede', Confirmed: 'Onaylandı',
      Completed: 'Tamamlandı', Cancelled: 'İptal', NoShow: 'Gelmedi'
    };
    return map[status] ?? status;
  }
}
