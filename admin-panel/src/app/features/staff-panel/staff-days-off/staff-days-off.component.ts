import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { CustomCalendarComponent } from '../../../shared/components/custom-calendar/custom-calendar.component';

interface DayOff {
  id: string;
  date: string;
  reason?: string;
}

@Component({
  selector: 'app-staff-days-off',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomCalendarComponent],
  templateUrl: './staff-days-off.component.html',
  styleUrl: './staff-days-off.component.scss',
})
export class StaffDaysOffComponent implements OnInit {
  daysOff: DayOff[] = [];
  isLoading = true;
  isSaving = false;
  errorMsg = '';

  form!: FormGroup;

  get todayDate(): string { return new Date().toISOString().split('T')[0]; }
  get markedDates(): string[] { return this.daysOff.map(d => d.date); }

  constructor(private http: HttpClient, private fb: FormBuilder) {
    this.form = this.fb.group({
      date:   ['', Validators.required],
      reason: [''],
    });
  }

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading = true;
    this.http.get<any>(`${environment.apiUrl}/staff-panel/days-off`).subscribe({
      next: (res) => {
        if (res.success) this.daysOff = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  add(): void {
    if (!this.form.valid) return;
    this.isSaving = true;
    this.errorMsg = '';
    const { date, reason } = this.form.value;
    this.http.post<any>(`${environment.apiUrl}/staff-panel/days-off`, { date, reason }).subscribe({
      next: (res) => {
        if (res.success) {
          this.form.reset();
          this.load();
        }
        this.isSaving = false;
      },
      error: (err) => {
        this.errorMsg = err.error?.message || 'İzin günü eklenemedi.';
        this.isSaving = false;
      },
    });
  }

  remove(id: string): void {
    this.http.delete<any>(`${environment.apiUrl}/staff-panel/days-off/${id}`).subscribe({
      next: () => this.load(),
    });
  }

  formatDate(d: string): string {
    return new Date(d + 'T12:00:00').toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });
  }
}
