import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppointmentService } from '../../core/services/appointment.service';
import { StaffService } from '../../core/services/staff.service';
import { CustomerService } from '../../core/services/customer.service';
import { ServiceService } from '../../core/services/service.service';
import { Appointment, AppointmentStatus } from '../../core/models/appointment.model';
import { EarningsDto, EarningsService } from '../../core/services/earnings.service';
import { LanguageService } from '../../core/services/language.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { AuthService } from '../../core/services/auth.service';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';

interface Stat {
  value: string | number;
  key: string;   // translation key
  icon: string;
  route: string;
  color: string;
}

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, TranslatePipe],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss',
})
export class DashboardComponent implements OnInit {
  todayAppointments: Appointment[] = [];
  pendingAppointments: Appointment[] = [];
  isLoading = true;
  isPendingLoading = true;
  earnings: EarningsDto | null = null;
  showEmailBanner = false;
  isResendingEmail = false;
  resendSuccess = false;

  AppointmentStatus = AppointmentStatus;

  stats: Stat[] = [
    { key: 'stat.todayAppts',     value: 0, icon: '📅', color: 'purple', route: '/appointments' },
    { key: 'stat.totalCustomers', value: 0, icon: '👥', color: 'blue',   route: '/customers'    },
    { key: 'stat.totalStaff',     value: 0, icon: '👤', color: 'green',  route: '/staff'        },
    { key: 'stat.totalServices',  value: 0, icon: '✂',  color: 'amber',  route: '/services'     },
    { key: 'stat.todayRevenue',   value: 0, icon: '💰', color: 'orange', route: '/reports'      },
  ];

  constructor(
    private appointmentService: AppointmentService,
    private staffService: StaffService,
    private customerService: CustomerService,
    private serviceService: ServiceService,
    private earningsService: EarningsService,
    public langService: LanguageService,
    private authService: AuthService,
    private http: HttpClient,
  ) {}

  ngOnInit(): void {
    this.showEmailBanner = !this.authService.isEmailVerified();
    this.loadDashboard();
  }

  resendVerificationEmail(): void {
    this.isResendingEmail = true;
    this.http.post<any>(`${environment.apiUrl}/auth/resend-verification`, {}).subscribe({
      next: () => {
        this.resendSuccess = true;
        this.isResendingEmail = false;
      },
      error: () => {
        this.isResendingEmail = false;
      }
    });
  }

  dismissEmailBanner(): void {
    this.showEmailBanner = false;
  }

  loadDashboard(): void {
    const today = new Date();
    const dateStr = new Date(Date.UTC(today.getFullYear(), today.getMonth(), today.getDate())).toISOString();

    this.appointmentService.getAll(undefined, dateStr).subscribe({
      next: (res) => {
        if (res.success) {
          this.todayAppointments = res.data;
          this.stats[0].value = res.data.length;
        }
        this.isLoading = false;
      },
    });

    this.appointmentService.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.pendingAppointments = res.data
            .filter(a => a.status === AppointmentStatus.Pending)
            .sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime());
        }
        this.isPendingLoading = false;
      },
      error: () => { this.isPendingLoading = false; }
    });

    const todayStart = new Date(Date.UTC(today.getFullYear(), today.getMonth(), today.getDate())).toISOString();
    const todayEnd   = new Date(Date.UTC(today.getFullYear(), today.getMonth(), today.getDate() + 1)).toISOString();

    this.earningsService.getEarnings(todayStart.split('T')[0], todayEnd.split('T')[0]).subscribe({
      next: (res) => {
        if (res.success) this.stats[4].value = Math.round(res.data.todayEarnings) + ' ₺';
      },
    });

    this.customerService.getAll().subscribe({
      next: (res) => { if (res.success) this.stats[1].value = res.data.length; },
    });

    this.staffService.getAll().subscribe({
      next: (res) => { if (res.success) this.stats[2].value = res.data.length; },
    });

    this.serviceService.getAll().subscribe({
      next: (res) => { if (res.success) this.stats[3].value = res.data.length; },
    });
  }

  getStatusKey(status: AppointmentStatus): string {
    const map: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]:   'status.pending',
      [AppointmentStatus.Confirmed]: 'status.confirmed',
      [AppointmentStatus.Completed]: 'status.completed',
      [AppointmentStatus.Cancelled]: 'status.cancelled',
      [AppointmentStatus.NoShow]:    'status.noShow',
    };
    return map[status] ?? 'status.pending';
  }

  getStatusClass(status: AppointmentStatus): string {
    const classes: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]:   'badge-warning',
      [AppointmentStatus.Confirmed]: 'badge-info',
      [AppointmentStatus.Completed]: 'badge-success',
      [AppointmentStatus.Cancelled]: 'badge-danger',
      [AppointmentStatus.NoShow]:    'badge-gray',
    };
    return classes[status] ?? 'badge-gray';
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString(this.langService.dateLocale, {
      hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul',
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString(this.langService.dateLocale, {
      day: 'numeric', month: 'short', timeZone: 'Europe/Istanbul',
    });
  }

  confirmAppointment(id: string): void {
    this.appointmentService.confirm(id).subscribe({
      next: () => {
        this.pendingAppointments = this.pendingAppointments.filter(a => a.id !== id);
        this.loadDashboard();
      },
      error: () => {
        this.pendingAppointments = this.pendingAppointments.filter(a => a.id !== id);
        this.loadDashboard();
      },
    });
  }

  completeAppointment(id: string): void {
    this.appointmentService.complete(id, { actualServiceIds: [] }).subscribe({
      next: () => this.loadDashboard(),
    });
  }
}
