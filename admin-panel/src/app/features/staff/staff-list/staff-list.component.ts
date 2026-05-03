import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  FormArray,
} from '@angular/forms';
import { StaffService } from '../../../core/services/staff.service';
import { Staff } from '../../../core/models/staff.model';
import {
  WorkingHoursService,
  WorkingHour,
} from '../../../core/services/working-hours.service';
import { Observable } from 'rxjs';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-staff-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, TranslatePipe],
  templateUrl: './staff-list.component.html',
  styleUrl: './staff-list.component.scss',
})
export class StaffListComponent implements OnInit {
  staffList: Staff[] = [];
  isLoading = true;
  isDrawerOpen = false;
  isSubmitting = false;
  editingStaff: Staff | null = null;
  errorMessage = '';
  isWorkingHoursOpen = false;
  selectedStaffForWH: Staff | null = null;
  workingHoursForm!: FormGroup;
  isSavingWH = false;
  staffForm: FormGroup;

  readonly days = [
    { value: 1 },
    { value: 2 },
    { value: 3 },
    { value: 4 },
    { value: 5 },
    { value: 6 },
    { value: 0 },
  ];

  constructor(
    private workingHoursService: WorkingHoursService,
    private staffService: StaffService,
    private fb: FormBuilder,
    public langService: LanguageService,
  ) {
    this.staffForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      phone: [''],
      bio: [''],
      isActive: [true],
    });
  }

  ngOnInit(): void {
    this.loadStaff();
  }

  getDayName(dayOfWeek: number): string {
    // 2024-01-07 = Sunday (0), 2024-01-08 = Monday (1), etc.
    const date = new Date(2024, 0, 7 + dayOfWeek);
    return new Intl.DateTimeFormat(this.langService.dateLocale, { weekday: 'long' }).format(date);
  }

  openWorkingHours(staff: Staff): void {
    this.selectedStaffForWH = staff;
    this.isWorkingHoursOpen = true;

    this.workingHoursForm = this.fb.group({
      hours: this.fb.array(
        this.days.map(day => this.fb.group({
          dayOfWeek: [day.value],
          startTime: ['09:00'],
          endTime:   ['18:00'],
          isOff:     [day.value === 0],
        }))
      )
    });

    this.workingHoursService.getByStaff(staff.id).subscribe({
      next: (res) => {
        if (res.success && res.data.length > 0) {
          const hoursArray = this.workingHoursForm.get('hours') as FormArray;
          res.data.forEach((wh: any) => {
            const idx = this.days.findIndex(d => d.value === wh.dayOfWeek);
            if (idx !== -1) {
              hoursArray.at(idx).patchValue({
                startTime: wh.startTime.slice(0, 5),
                endTime:   wh.endTime.slice(0, 5),
                isOff:     wh.isOff
              });
            }
          });
        }
      }
    });
  }

  closeWorkingHours(): void {
    this.isWorkingHoursOpen = false;
    this.selectedStaffForWH = null;
  }

  get hoursArray(): FormArray {
    return this.workingHoursForm?.get('hours') as FormArray;
  }

  saveWorkingHours(): void {
    if (!this.selectedStaffForWH) return;
    this.isSavingWH = true;

    const staffId = this.selectedStaffForWH.id;
    const hours   = this.hoursArray.value;

    this.workingHoursService.getByStaff(staffId).subscribe({
      next: (res) => {
        const existing = res.data || [];
        const requests = hours.map((h: any) => {
          const found = existing.find((e: any) => e.dayOfWeek === h.dayOfWeek);
          const endTime = h.endTime === '00:00' ? '23:59:00' : h.endTime + ':00';
          const data: WorkingHour = {
            staffId,
            dayOfWeek: h.dayOfWeek,
            startTime: h.startTime + ':00',
            endTime:   endTime,
            isOff:     h.isOff
          };
          return found
            ? this.workingHoursService.update(found.id, data)
            : this.workingHoursService.create(data);
        });

        Promise.all(requests.map((r: Observable<any>) => r.toPromise()))
          .then(() => { this.isSavingWH = false; this.closeWorkingHours(); })
          .catch(() => { this.isSavingWH = false; });
      }
    });
  }

  loadStaff(): void {
    this.isLoading = true;
    this.staffService.getAll().subscribe({
      next: (res) => {
        if (res.success) this.staffList = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  openDrawer(staff?: Staff): void {
    this.editingStaff = staff || null;
    this.errorMessage = '';
    if (staff) {
      this.staffForm.patchValue(staff);
    } else {
      this.staffForm.reset({ isActive: true });
    }
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
    this.editingStaff = null;
    this.staffForm.reset({ isActive: true });
  }

  onSubmit(): void {
    if (this.staffForm.invalid) return;
    this.isSubmitting = true;
    this.errorMessage = '';
    const value = this.staffForm.value;

    if (this.editingStaff) {
      this.staffService.update(this.editingStaff.id, value).subscribe({
        next: (res) => {
          if (res.success) { this.loadStaff(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hata oluştu.';
          this.isSubmitting = false;
        },
      });
    } else {
      this.staffService.create(value).subscribe({
        next: (res) => {
          if (res.success) { this.loadStaff(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hata oluştu.';
          this.isSubmitting = false;
        },
      });
    }
  }

  deleteStaff(id: string): void {
    if (!confirm(this.langService.t('staff.edit') + '?')) return;
    this.staffService.delete(id).subscribe({
      next: () => this.loadStaff(),
    });
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}