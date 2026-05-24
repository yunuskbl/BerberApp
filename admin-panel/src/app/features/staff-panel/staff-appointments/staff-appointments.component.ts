import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface StaffAppointment {
  id: string;
  startTime: string;
  endTime: string;
  status: string;
  customerName: string;
  customerPhone: string;
  serviceName: string;
  notes?: string;
}

@Component({
  selector: 'app-staff-appointments',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './staff-appointments.component.html',
  styleUrl: './staff-appointments.component.scss',
})
export class StaffAppointmentsComponent implements OnInit {
  appointments: StaffAppointment[] = [];
  isLoading = true;
  today = new Date().toISOString().split('T')[0];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.loadAppointments();
  }

  loadAppointments(): void {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/staff-panel/appointments`).subscribe({
      next: (res) => {
        if (res.success) this.appointments = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  formatDate(dt: string): string {
    return new Date(dt).toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  formatTime(dt: string): string {
    return new Date(dt).toLocaleTimeString('tr-TR', { hour: '2-digit', minute: '2-digit' });
  }

  isToday(dt: string): boolean {
    return new Date(dt).toISOString().split('T')[0] === this.today;
  }

  isPast(dt: string): boolean {
    return new Date(dt) < new Date();
  }

  statusLabel(status: string): string {
    const map: Record<string, string> = {
      Pending: 'Bekliyor', Confirmed: 'Onaylı',
      Completed: 'Tamamlandı', Cancelled: 'İptal',
    };
    return map[status] ?? status;
  }

  statusClass(status: string): string {
    const map: Record<string, string> = {
      Pending: 'pending', Confirmed: 'confirmed',
      Completed: 'completed', Cancelled: 'cancelled',
    };
    return map[status] ?? '';
  }

  get upcoming(): StaffAppointment[] {
    return this.appointments.filter(a => !this.isPast(a.startTime) || this.isToday(a.startTime));
  }

  get past(): StaffAppointment[] {
    return this.appointments.filter(a => this.isPast(a.startTime) && !this.isToday(a.startTime));
  }
}
