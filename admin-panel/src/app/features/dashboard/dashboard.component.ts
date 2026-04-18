import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { AppointmentService } from '../../core/services/appointment.service';
import { StaffService } from '../../core/services/staff.service';
import { CustomerService } from '../../core/services/customer.service';
import { ServiceService } from '../../core/services/service.service';
import { Appointment, AppointmentStatus } from '../../core/models/appointment.model';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './dashboard.component.html',
  styleUrl: './dashboard.component.scss'
})
export class DashboardComponent implements OnInit {
  todayAppointments: Appointment[] = [];
  totalCustomers  = 0;
  totalStaff      = 0;
  totalServices   = 0;
  isLoading       = true;

  AppointmentStatus = AppointmentStatus;

  stats = [
    { label: 'Bugünkü Randevular', value: 0, icon: '📅', color: 'purple', route: '/appointments' },
    { label: 'Toplam Müşteri',     value: 0, icon: '👥', color: 'blue',   route: '/customers'   },
    { label: 'Toplam Personel',    value: 0, icon: '👤', color: 'green',  route: '/staff'       },
    { label: 'Toplam Hizmet',      value: 0, icon: '✂',  color: 'amber',  route: '/services'   },
  ];

  constructor(
    private appointmentService: AppointmentService,
    private staffService: StaffService,
    private customerService: CustomerService,
    private serviceService: ServiceService
  ) {}

  ngOnInit(): void {
    this.loadDashboard();
  }

 loadDashboard(): void {
  // Bugünkü randevular
  const todayStart = new Date();
  todayStart.setHours(0, 0, 0, 0);
  const dateStr = todayStart.toISOString();

  this.appointmentService.getAll(undefined, dateStr).subscribe({
    next: (res) => {
      if (res.success) {
        this.todayAppointments = res.data;
        this.stats[0].value    = res.data.length;
      }
      this.isLoading = false;
    }
  });

  // Müşteriler
  this.customerService.getAll().subscribe({
    next: (res) => {
      if (res.success) {
        this.totalCustomers = res.data.length;
        this.stats[1].value = res.data.length;
      }
    }
  });

  // Personel
  this.staffService.getAll().subscribe({
    next: (res) => {
      if (res.success) {
        this.totalStaff     = res.data.length;
        this.stats[2].value = res.data.length;
      }
    }
  });

  // Hizmetler
  this.serviceService.getAll().subscribe({
    next: (res) => {
      if (res.success) {
        this.totalServices  = res.data.length;
        this.stats[3].value = res.data.length;
      }
    }
  });
}
  getStatusLabel(status: AppointmentStatus): string {
    const labels: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]:   'Bekliyor',
      [AppointmentStatus.Confirmed]: 'Onaylandı',
      [AppointmentStatus.Completed]: 'Tamamlandı',
      [AppointmentStatus.Cancelled]: 'İptal',
      [AppointmentStatus.NoShow]:    'Gelmedi'
    };
    return labels[status] ?? 'Bilinmiyor';
  }

  getStatusClass(status: AppointmentStatus): string {
    const classes: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]:   'badge-warning',
      [AppointmentStatus.Confirmed]: 'badge-info',
      [AppointmentStatus.Completed]: 'badge-success',
      [AppointmentStatus.Cancelled]: 'badge-danger',
      [AppointmentStatus.NoShow]:    'badge-gray'
    };
    return classes[status] ?? 'badge-gray';
  }

  formatTime(dateStr: string): string {
  return new Date(dateStr).toLocaleTimeString('tr-TR', {
    hour:   '2-digit',
    minute: '2-digit',
    timeZone: 'Europe/Istanbul'
  });
}
}