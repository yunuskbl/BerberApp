import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';

interface WorkingHour {
  id: string;
  /** Sunucu gün adını metin ("Monday") olarak gönderiyor; eski kayıtlarda sayı olabilir. */
  dayOfWeek: number | string;
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
        // Haftayı pazartesiden başlat (Türkiye'de alışılan sıra).
        if (res.success) this.hours = [...res.data].sort((a: WorkingHour, b: WorkingHour) =>
          (this.dayIndex(a.dayOfWeek) + 6) % 7 - (this.dayIndex(b.dayOfWeek) + 6) % 7);
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  private static readonly EN = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  /** 0 = pazar … 6 = cumartesi; sayı ya da İngilizce gün adı kabul eder. */
  dayIndex(day: number | string): number {
    if (typeof day === 'number') return day;
    const i = StaffWorkingHoursComponent.EN.indexOf(day);
    return i >= 0 ? i : Number(day);
  }

  dayName(day: number | string): string {
    return this.days[this.dayIndex(day)] ?? String(day);
  }

  formatTime(t: string): string {
    return t?.slice(0, 5) ?? '--:--';
  }
}
