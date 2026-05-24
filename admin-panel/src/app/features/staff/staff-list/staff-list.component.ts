import { Component, OnInit } from '@angular/core';
import { CommonModule, CurrencyPipe, DatePipe } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  FormArray,
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { StaffService } from '../../../core/services/staff.service';
import { ServiceService } from '../../../core/services/service.service';
import { Staff } from '../../../core/models/staff.model';
import { Service } from '../../../core/models/service.model';
import {
  WorkingHoursService,
  WorkingHour,
} from '../../../core/services/working-hours.service';
import { StaffDaysOffService, StaffDayOff } from '../../../core/services/staff-days-off.service';
import { Observable, catchError, of } from 'rxjs';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';
import { CustomCalendarComponent } from '../../../shared/components/custom-calendar/custom-calendar.component';
import { environment } from '../../../../environments/environment';

@Component({
  selector: 'app-staff-list',
  standalone: true,
  imports: [CommonModule, CurrencyPipe, DatePipe, ReactiveFormsModule, TranslatePipe, CustomCalendarComponent],
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

  isDaysOffOpen = false;
  selectedStaffForDaysOff: Staff | null = null;
  staffDaysOff: StaffDayOff[] = [];
  isDaysOffLoading = false;
  isDaysOffSaving = false;
  daysOffError = '';
  daysOffForm!: FormGroup;

  // ── Hesap Oluşturma ───────────────────────────────────────────────────────
  isAccountModalOpen = false;
  selectedStaffForAccount: Staff | null = null;
  accountForm!: FormGroup;
  isCreatingAccount = false;
  accountSuccess = '';
  accountError = '';
  staffWithAccounts = new Set<string>(); // hesabı olan personel ID'leri

  isServicesOpen = false;
  selectedStaffForSvc: Staff | null = null;
  allServices: Service[] = [];
  assignedServices: Map<string, { customPrice: number | null; customCurrency: string | null; customDurationMinutes: number | null }> = new Map();
  readonly currencies = ['TRY', 'USD', 'EUR', 'GBP'];
  isSavingSvc = false;
  svcErrorMessage = '';
  svcSuccessMessage = '';

  staffForm: FormGroup;

  readonly days = [
    { value: 1 }, { value: 2 }, { value: 3 }, { value: 4 },
    { value: 5 }, { value: 6 }, { value: 0 },
  ];

  get todayDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  get markedDayOffDates(): string[] {
    return this.staffDaysOff.map(d => d.date);
  }

  formatDayOffDate(dateStr: string): string {
    if (!dateStr) return '';
    const d = new Date(dateStr + 'T12:00:00');
    return d.toLocaleDateString('tr-TR', { day: 'numeric', month: 'long', year: 'numeric' });
  }

  constructor(
    private workingHoursService: WorkingHoursService,
    private staffService: StaffService,
    private serviceService: ServiceService,
    private staffDaysOffService: StaffDaysOffService,
    private fb: FormBuilder,
    private http: HttpClient,
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

  // ── Çalışma Saatleri ───────────────────────────────────────────────────────

  getDayName(dayOfWeek: number): string {
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

  // ── İzin Günleri ──────────────────────────────────────────────────────────

  openDaysOff(staff: Staff): void {
    this.selectedStaffForDaysOff = staff;
    this.isDaysOffOpen = true;
    this.daysOffError = '';
    this.daysOffForm = this.fb.group({
      date: ['', Validators.required],
      reason: [''],
    });
    this.loadDaysOff();
  }

  closeDaysOff(): void {
    this.isDaysOffOpen = false;
    this.selectedStaffForDaysOff = null;
    this.staffDaysOff = [];
  }

  loadDaysOff(): void {
    if (!this.selectedStaffForDaysOff) return;
    this.isDaysOffLoading = true;
    this.staffDaysOffService.getByStaff(this.selectedStaffForDaysOff.id).pipe(
      catchError(() => of({ success: false, data: [] }))
    ).subscribe({
      next: (res) => {
        this.staffDaysOff = res.success && Array.isArray(res.data) ? res.data : [];
        this.isDaysOffLoading = false;
      },
    });
  }

  addDayOff(): void {
    if (!this.daysOffForm.valid || !this.selectedStaffForDaysOff) return;
    this.isDaysOffSaving = true;
    this.daysOffError = '';
    const { date, reason } = this.daysOffForm.value;
    this.staffDaysOffService.create(this.selectedStaffForDaysOff.id, { date, reason: reason || undefined }).pipe(
      catchError((err) => {
        this.daysOffError = err?.error?.message || 'İzin günü eklenemedi.';
        this.isDaysOffSaving = false;
        return of(null);
      })
    ).subscribe({
      next: (res) => {
        if (!res) return;
        this.isDaysOffSaving = false;
        this.daysOffForm.reset();
        this.loadDaysOff();
      },
    });
  }

  removeDayOff(id: string): void {
    if (!this.selectedStaffForDaysOff) return;
    this.staffDaysOffService.delete(this.selectedStaffForDaysOff.id, id).pipe(
      catchError(() => of(null))
    ).subscribe({
      next: () => this.loadDaysOff(),
    });
  }

  // ── Hizmet Ataması ─────────────────────────────────────────────────────────

  openServices(staff: Staff): void {
    this.selectedStaffForSvc = staff;
    this.assignedServices = new Map();
    this.svcErrorMessage = '';
    this.isServicesOpen = true;

    this.serviceService.getAll().subscribe({
      next: (res) => { if (res.success) this.allServices = res.data; }
    });

    this.staffService.getServices(staff.id).subscribe({
      next: (res) => {
        if (res.success && Array.isArray(res.data)) {
          this.assignedServices = new Map(
            res.data.map((item: any) => [item.serviceId, {
              customPrice: item.customPrice ?? null,
              customCurrency: item.customCurrency ?? null,
              customDurationMinutes: item.customDurationMinutes ?? null
            }])
          );
        }
      }
    });
  }

  closeServices(): void {
    this.isServicesOpen = false;
    this.selectedStaffForSvc = null;
  }

  toggleService(serviceId: string): void {
    if (this.assignedServices.has(serviceId))
      this.assignedServices.delete(serviceId);
    else
      this.assignedServices.set(serviceId, { customPrice: null, customCurrency: null, customDurationMinutes: null });
  }

  isServiceAssigned(serviceId: string): boolean {
    return this.assignedServices.has(serviceId);
  }

  getServiceCustomPrice(serviceId: string): number | null {
    return this.assignedServices.get(serviceId)?.customPrice ?? null;
  }

  setServiceCustomPrice(serviceId: string, value: string): void {
    const entry = this.assignedServices.get(serviceId);
    if (!entry) return;
    const parsed = value ? parseFloat(value) : null;
    entry.customPrice = parsed !== null && !isNaN(parsed) ? parsed : null;
  }

  getServiceCustomCurrency(serviceId: string): string | null {
    return this.assignedServices.get(serviceId)?.customCurrency ?? null;
  }

  setServiceCustomCurrency(serviceId: string, value: string): void {
    const entry = this.assignedServices.get(serviceId);
    if (!entry) return;
    entry.customCurrency = value || null;
  }

  saveServices(): void {
    if (!this.selectedStaffForSvc) return;
    this.isSavingSvc = true;

    const items = Array.from(this.assignedServices.entries()).map(([serviceId, data]) => {
      const svc = this.allServices.find(s => s.id === serviceId);
      return {
        serviceId,
        customPrice: data.customPrice,
        customCurrency: data.customPrice != null
          ? (data.customCurrency ?? svc?.currency ?? null)
          : null,
        customDurationMinutes: data.customDurationMinutes
      };
    });

    this.staffService.setServices(this.selectedStaffForSvc.id, items).subscribe({
      next: () => {
        this.isSavingSvc = false;
        this.svcSuccessMessage = 'Hizmetler başarıyla kaydedildi.';
        setTimeout(() => { this.svcSuccessMessage = ''; this.closeServices(); }, 2000);
      },
      error: (err) => {
        this.isSavingSvc = false;
        this.svcErrorMessage = err?.error?.message || 'Hizmetler kaydedilemedi. Lütfen tekrar deneyin.';
      }
    });
  }

  // ── CRUD ───────────────────────────────────────────────────────────────────

  loadStaff(): void {
    this.isLoading = true;
    this.staffService.getAll().subscribe({
      next: (res) => {
        if (res.success) {
          this.staffList = res.data;
          this.checkStaffAccounts();
        }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  checkStaffAccounts(): void {
    this.staffList.forEach(s => {
      this.http.get<any>(`${environment.apiUrl}/staff/${s.id}/has-account`).subscribe({
        next: (res) => { if (res?.data?.hasAccount) this.staffWithAccounts.add(s.id); },
        error: () => {},
      });
    });
  }

  hasAccount(staffId: string): boolean {
    return this.staffWithAccounts.has(staffId);
  }

  openAccountModal(staff: Staff): void {
    this.selectedStaffForAccount = staff;
    this.accountSuccess = '';
    this.accountError = '';
    this.accountForm = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', [Validators.required, Validators.minLength(8)]],
    });
    this.isAccountModalOpen = true;
  }

  closeAccountModal(): void {
    this.isAccountModalOpen = false;
    this.selectedStaffForAccount = null;
  }

  createAccount(): void {
    if (!this.accountForm.valid || !this.selectedStaffForAccount) return;
    this.isCreatingAccount = true;
    this.accountError = '';
    const { email, password } = this.accountForm.value;
    this.http.post<any>(`${environment.apiUrl}/staff/${this.selectedStaffForAccount.id}/create-account`, { email, password }).subscribe({
      next: (res) => {
        if (res.success) {
          this.accountSuccess = `Hesap oluşturuldu: ${email}`;
          this.staffWithAccounts.add(this.selectedStaffForAccount!.id);
          setTimeout(() => this.closeAccountModal(), 2000);
        }
        this.isCreatingAccount = false;
      },
      error: (err) => {
        this.accountError = err.error?.message || 'Hesap oluşturulamadı.';
        this.isCreatingAccount = false;
      },
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
