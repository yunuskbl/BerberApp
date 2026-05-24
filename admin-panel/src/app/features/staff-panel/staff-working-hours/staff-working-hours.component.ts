import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface WorkingHour {
  id: string;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  isOff: boolean;
}

@Component({
  selector: 'app-staff-working-hours',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './staff-working-hours.component.html',
  styleUrl: './staff-working-hours.component.scss',
})
export class StaffWorkingHoursComponent implements OnInit {
  hours: WorkingHour[] = [];
  isLoading = true;

  readonly days = ['Pazar', 'Pazartesi', 'Salı', 'Çarşamba', 'Perşembe', 'Cuma', 'Cumartesi'];

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    this.http.get<any>(`${environment.apiUrl}/staff-panel/working-hours`).subscribe({
      next: (res) => {
        if (res.success) this.hours = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  dayName(day: number): string {
    return this.days[day] ?? `Gün ${day}`;
  }

  formatTime(t: string): string {
    return t?.slice(0, 5) ?? '--:--';
  }
}
