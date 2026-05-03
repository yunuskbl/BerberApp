import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { AppointmentService } from '../../../core/services/appointment.service';
import { StaffService } from '../../../core/services/staff.service';
import { CustomerService } from '../../../core/services/customer.service';
import { ServiceService } from '../../../core/services/service.service';
import {
  Appointment,
  AppointmentStatus,
  AvailableSlot,
} from '../../../core/models/appointment.model';
import { Staff } from '../../../core/models/staff.model';
import { Customer } from '../../../core/models/customer.model';
import { Service } from '../../../core/models/service.model';
import {
  CustomSelectComponent,
  SelectOption,
} from '../../../shared/components/custom-select/custom-select.component';
import { CustomCalendarComponent } from '../../../shared/components/custom-calendar/custom-calendar.component';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-appointment-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomSelectComponent,
    CustomCalendarComponent,
    TranslatePipe,
  ],
  templateUrl: './appointment-list.component.html',
  styleUrl: './appointment-list.component.scss',
})
export class AppointmentListComponent implements OnInit {
  appointments: Appointment[] = [];
  staffList: Staff[] = [];
  customerList: Customer[] = [];
  serviceList: Service[] = [];
  availableSlots: AvailableSlot[] = [];

  isLoading = true;
  isDrawerOpen = false;
  isSubmitting = false;
  isLoadingSlots = false;
  errorMessage = '';

  selectedDate = new Date().toISOString().split('T')[0];
  selectedStaffId = '';

  viewMode: 'list' | 'calendar' = 'list';
  readonly hourHeight = 64;
  readonly calendarStart = 8;
  readonly calendarEnd = 21;
  readonly calendarHours = Array.from({ length: 21 - 8 }, (_, i) => i + 8);

  AppointmentStatus = AppointmentStatus;

  appointmentForm: FormGroup;

  constructor(
    private appointmentService: AppointmentService,
    private staffService: StaffService,
    private customerService: CustomerService,
    private serviceService: ServiceService,
    private fb: FormBuilder,
    public langService: LanguageService,
  ) {
    this.appointmentForm = this.fb.group({
      customerId: ['', Validators.required],
      staffId: ['', Validators.required],
      serviceId: ['', Validators.required],
      date: ['', Validators.required],
      startTime: ['', Validators.required],
      notes: [''],
    });
  }

  ngOnInit(): void {
    this.loadAll();
  }

  loadAll(): void {
    this.loadAppointments();
    this.staffService.getAll().subscribe(r => { if (r.success) this.staffList = r.data; });
    this.customerService.getAll().subscribe(r => { if (r.success) this.customerList = r.data; });
    this.serviceService.getAll().subscribe(r => { if (r.success) this.serviceList = r.data; });
  }

  isDatePickerOpen = false;

  get filterDateDisplay(): string {
    if (!this.selectedDate) return this.langService.t('appt.chooseDate');
    const d = new Date(this.selectedDate);
    return d.toLocaleDateString(this.langService.dateLocale, { day: 'numeric', month: 'long' });
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    if (!target.closest('.filter-date-wrapper')) {
      this.isDatePickerOpen = false;
    }
  }

  onFilterDateChange(value: string): void {
    this.selectedDate = value;
    this.isDatePickerOpen = false;
    this.loadAppointments();
  }

  get allStaffOptions(): SelectOption[] {
    return [{ value: '', label: this.langService.t('appt.allStaff') }, ...this.staffOptions];
  }

  get minDate(): string { return new Date().toISOString().split('T')[0]; }

  get maxDate(): string {
    const date = new Date();
    date.setDate(date.getDate() + 15);
    return date.toISOString().split('T')[0];
  }

  loadAppointments(): void {
    this.isLoading = true;
    let dateStr: string | undefined;
    if (this.selectedDate) {
      const [year, month, day] = this.selectedDate.split('-').map(Number);
      dateStr = new Date(Date.UTC(year, month - 1, day, 0, 0, 0)).toISOString();
    }
    this.appointmentService.getAll(this.selectedStaffId || undefined, dateStr).subscribe({
      next: (res) => {
        if (res.success) this.appointments = res.data;
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  onDateChange(event: Event): void {
    this.selectedDate = (event.target as HTMLInputElement).value;
    this.loadAppointments();
  }

  onStaffFilter(event: Event): void {
    this.selectedStaffId = (event.target as HTMLSelectElement).value;
    this.loadAppointments();
  }

  openDrawer(): void {
    this.errorMessage = '';
    this.availableSlots = [];
    this.appointmentForm.reset({ date: this.selectedDate });
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
    this.availableSlots = [];
    this.appointmentForm.reset();
  }

  onFormFieldChange(): void {
    const { staffId, serviceId, date } = this.appointmentForm.value;
    if (staffId && serviceId && date) this.loadAvailableSlots(staffId, serviceId, date);
  }

  loadAvailableSlots(staffId: string, serviceId: string, date: string): void {
    this.isLoadingSlots = true;
    this.availableSlots = [];
    this.appointmentForm.patchValue({ startTime: '' });
    this.appointmentService.getAvailableSlots(staffId, serviceId, new Date(date).toISOString()).subscribe({
      next: (res) => {
        if (res.success) this.availableSlots = res.data.filter((s: any) => s.isAvailable);
        this.isLoadingSlots = false;
      },
      error: () => { this.isLoadingSlots = false; }
    });
  }

  onSubmit(): void {
    if (this.appointmentForm.invalid) return;
    this.isSubmitting = true;
    this.errorMessage = '';
    const { customerId, staffId, serviceId, date, startTime, notes } = this.appointmentForm.value;
    this.appointmentService.create({ customerId, staffId, serviceId, startTime: `${date}T${startTime}`, notes }).subscribe({
      next: (res) => {
        if (res.success) { this.loadAppointments(); this.closeDrawer(); }
        this.isSubmitting = false;
      },
      error: (err) => { this.errorMessage = err.error?.message || 'Hata oluştu.'; this.isSubmitting = false; }
    });
  }

  cancelAppointment(id: string): void {
    if (!confirm('?')) return;
    this.appointmentService.cancel(id).subscribe({ next: () => this.loadAppointments() });
  }

  completeAppointment(id: string): void {
    this.appointmentService.complete(id).subscribe({ next: () => this.loadAppointments() });
  }

  confirmAppointment(id: string): void {
    this.appointmentService.confirm(id).subscribe({
      next: () => this.loadAppointments(),
      error: (err) => console.error('Confirm error:', err)
    });
  }

  getStatusLabel(status: AppointmentStatus): string {
    const map: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]:   this.langService.t('status.pending'),
      [AppointmentStatus.Confirmed]: this.langService.t('status.confirmed'),
      [AppointmentStatus.Completed]: this.langService.t('status.completed'),
      [AppointmentStatus.Cancelled]: this.langService.t('status.cancelled'),
      [AppointmentStatus.NoShow]:    this.langService.t('status.noShow'),
    };
    return map[status] ?? '—';
  }

  getStatusClass(status: AppointmentStatus): string {
    const map: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]:   'badge-warning',
      [AppointmentStatus.Confirmed]: 'badge-info',
      [AppointmentStatus.Completed]: 'badge-success',
      [AppointmentStatus.Cancelled]: 'badge-danger',
      [AppointmentStatus.NoShow]:    'badge-gray',
    };
    return map[status] ?? 'badge-gray';
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString(this.langService.dateLocale, {
      hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul'
    });
  }

  formatSlotTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString(this.langService.dateLocale, {
      hour: '2-digit', minute: '2-digit',
    });
  }

  formatSlotValue(dateStr: string): string {
    return new Date(dateStr).toTimeString().slice(0, 5);
  }

  private getTurkeyHM(dateStr: string): { h: number; m: number } {
    const timeStr = new Date(dateStr).toLocaleTimeString('en-US', {
      hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Europe/Istanbul',
    });
    const [h, m] = timeStr.split(':').map(Number);
    return { h, m };
  }

  getAptBlockStyle(apt: Appointment): { [key: string]: string } {
    const { h, m } = this.getTurkeyHM(apt.startTime);
    const minutesFromStart = (h - this.calendarStart) * 60 + m;
    return {
      top: `${(minutesFromStart / 60) * this.hourHeight}px`,
      height: `${Math.max((apt.durationMinutes / 60) * this.hourHeight, 28)}px`,
    };
  }

  get calendarTotalHeight(): number {
    return (this.calendarEnd - this.calendarStart) * this.hourHeight;
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  get staffOptions(): SelectOption[] {
    return this.staffList.map(s => ({ value: s.id, label: s.fullName }));
  }

  get customerOptions(): SelectOption[] {
    return this.customerList.map(c => ({ value: c.id, label: `${c.fullName} — ${c.phone}` }));
  }

  get serviceOptions(): SelectOption[] {
    const min = this.langService.t('common.min');
    return this.serviceList.map(s => ({ value: s.id, label: `${s.name} — ${s.durationMinutes} ${min}` }));
  }

  onSelectChange(field: string, value: string): void {
    this.appointmentForm.patchValue({ [field]: value });
    if (field === 'staffId' || field === 'serviceId') this.onFormFieldChange();
  }
}