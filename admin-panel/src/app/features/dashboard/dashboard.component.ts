import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppointmentService } from '../../core/services/appointment.service';
import { StaffService } from '../../core/services/staff.service';
import { CustomerService } from '../../core/services/customer.service';
import { ServiceService } from '../../core/services/service.service';
import {
  Appointment,
  AppointmentStatus,
} from '../../core/models/appointment.model';
import {
  EarningsDto,
  EarningsService,
} from '../../core/services/earnings.service';

interface Stat {
  value: string | number;
  label: string;
  icon: string;
  route: string;
  color: string;
}
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  todayAppointments: Appointment[] = [];
  totalCustomers = 0;
  totalStaff = 0;
  totalServices = 0;
  isLoading = true;

  earnings: EarningsDto | null = null;
  isLoadingEarnings = false;
  reportStartDate = this.getDateString(
    new Date(new Date().setDate(new Date().getDate() - 30)),
  );
  reportEndDate = this.getDateString(new Date());

  AppointmentStatus = AppointmentStatus;

  stats: Stat[] = [
    {
      label: 'Bugünkü Randevular',
      value: 0,
      icon: '📅',
      color: 'purple',
      route: '/appointments',
    },
    {
      label: 'Toplam Müşteri',
      value: 0,
      icon: '👥',
      color: 'blue',
      route: '/customers',
    },
    {
      label: 'Toplam Personel',
      value: 0,
      icon: '👤',
      color: 'green',
      route: '/staff',
    },
    {
      label: 'Toplam Hizmet',
      value: 0,
      icon: '✂',
      color: 'amber',
      route: '/services',
    },
    {
      label: 'Gelir Raporu',
      value: 0,
      icon: '💰',
      color: 'orange',
      route: '/reports',
    },
  ];

  constructor(
    private appointmentService: AppointmentService,
    private staffService: StaffService,
    private customerService: CustomerService,
    private serviceService: ServiceService,
    private earningsService: EarningsService,
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

  loadDashboard(): void {
    const today = new Date();
    const year = today.getFullYear();
    const month = today.getMonth();
    const day = today.getDate();
    const dateStr = new Date(Date.UTC(year, month, day, 0, 0, 0)).toISOString();

    this.appointmentService.getAll(undefined, dateStr).subscribe({
      next: (res) => {
        if (res.success) {
          this.todayAppointments = res.data;
          this.stats[0].value = res.data.length;
        }
        this.isLoading = false;
      },
    });
    // Report verileri
    const todayStart = new Date(
      Date.UTC(today.getFullYear(), today.getMonth(), today.getDate(), 0, 0, 0),
    ).toISOString();
    const todayEnd = new Date(
      Date.UTC(
        today.getFullYear(),
        today.getMonth(),
        today.getDate() + 1,0,0,0,
      ),
    ).toISOString();

    this.earningsService
      .getEarnings(todayStart.split('T')[0], todayEnd.split('T')[0])
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.stats[4].value= (Math.round(res.data.todayEarnings))+" ₺"; // Bugünkü geliri göster
            this.stats[4].label="Bugünkü Gelir"; // Label'ı güncelle
          }
        },
      });
    // Müşteriler
    this.customerService.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.totalCustomers = res.data.length;
          this.stats[1].value = res.data.length;
        }
      },
    });

    // Personel
    this.staffService.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.totalStaff = res.data.length;
          this.stats[2].value = res.data.length;
        }
      },
    });

    // Hizmetler
    this.serviceService.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.totalServices = res.data.length;
          this.stats[3].value = res.data.length;
        }
      },
    });
  }
  getStatusLabel(status: AppointmentStatus): string {
    const labels: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]: 'Bekliyor',
      [AppointmentStatus.Confirmed]: 'Onaylandı',
      [AppointmentStatus.Completed]: 'Tamamlandı',
      [AppointmentStatus.Cancelled]: 'İptal',
      [AppointmentStatus.NoShow]: 'Gelmedi',
    };
    return labels[status] ?? 'Bilinmiyor';
  }

  getStatusClass(status: AppointmentStatus): string {
    const classes: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]: 'badge-warning',
      [AppointmentStatus.Confirmed]: 'badge-info',
      [AppointmentStatus.Completed]: 'badge-success',
      [AppointmentStatus.Cancelled]: 'badge-danger',
      [AppointmentStatus.NoShow]: 'badge-gray',
    };
    return classes[status] ?? 'badge-gray';
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Europe/Istanbul',
    });
  }
  // Rapor verilerini yükle
  loadEarnings(): void {
    this.isLoadingEarnings = true;
    this.earningsService
      .getEarnings(this.reportStartDate, this.reportEndDate)
      .subscribe({
        next: (res) => {
          if (res.success) this.earnings = res.data;
          this.isLoadingEarnings = false;
        },
        error: () => {
          this.isLoadingEarnings = false;
        },
      });
  }

  private getDateString(date: Date): string {
    return date.toISOString().split('T')[0];
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: 'TRY',
      minimumFractionDigits: 0,
    }).format(value);
  }
}
