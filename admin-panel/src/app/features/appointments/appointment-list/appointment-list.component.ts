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

@Component({
  selector: 'app-appointment-list',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomSelectComponent,
    CustomCalendarComponent,
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

  AppointmentStatus = AppointmentStatus;

  appointmentForm: FormGroup;

  constructor(
    private appointmentService: AppointmentService,
    private staffService: StaffService,
    private customerService: CustomerService,
    private serviceService: ServiceService,
    private fb: FormBuilder,
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
    this.staffService.getAll().subscribe((r) => {
      if (r.success) this.staffList = r.data;
    });
    this.customerService.getAll().subscribe((r) => {
      if (r.success) this.customerList = r.data;
    });
    this.serviceService.getAll().subscribe((r) => {
      if (r.success) this.serviceList = r.data;
    });
  }
  isDatePickerOpen = false;

  get filterDateDisplay(): string {
    if (!this.selectedDate) return 'Tarih Seç';
    const d = new Date(this.selectedDate);
    return d.toLocaleDateString('tr-TR', {
      day: 'numeric',
      month: 'long',
    });
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
    return [{ value: '', label: 'Tüm Personel' }, ...this.staffOptions];
  }
  get minDate(): string {
    return new Date().toISOString().split('T')[0];
  }

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
    const d = new Date(Date.UTC(year, month - 1, day, 0, 0, 0));
    dateStr = d.toISOString(); // → 2026-04-22T00:00:00.000Z
  }

  this.appointmentService.getAll(
    this.selectedStaffId || undefined,
    dateStr
  ).subscribe({
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
    this.appointmentForm.reset({
      date: this.selectedDate,
    });
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
    this.availableSlots = [];
    this.appointmentForm.reset();
  }

  onFormFieldChange(): void {
    const { staffId, serviceId, date } = this.appointmentForm.value;
    if (staffId && serviceId && date) {
      this.loadAvailableSlots(staffId, serviceId, date);
    }
  }

  loadAvailableSlots(staffId: string, serviceId: string, date: string): void {
    this.isLoadingSlots = true;
    this.availableSlots = [];
    this.appointmentForm.patchValue({ startTime: '' });

    const dateStr = new Date(date).toISOString();

    this.appointmentService
      .getAvailableSlots(staffId, serviceId, dateStr)
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.availableSlots = res.data.filter((s) => s.isAvailable);
          }
          this.isLoadingSlots = false;
        },
        error: () => {
          this.isLoadingSlots = false;
        },
      });
  }

  onSubmit(): void {
    if (this.appointmentForm.invalid) return;

    this.isSubmitting = true;
    this.errorMessage = '';

    const { customerId, staffId, serviceId, date, startTime, notes } =
      this.appointmentForm.value;
    const startDateTime = `${date}T${startTime}`;

    this.appointmentService
      .create({
        customerId,
        staffId,
        serviceId,
        startTime: startDateTime,
        notes,
      })
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.loadAppointments();
            this.closeDrawer();
          }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Hata oluştu.';
          this.isSubmitting = false;
        },
      });
  }

  cancelAppointment(id: string): void {
    if (!confirm('Randevuyu iptal etmek istediğinizden emin misiniz?')) return;
    this.appointmentService.cancel(id).subscribe({
      next: () => this.loadAppointments(),
    });
  }

  completeAppointment(id: string): void {
    this.appointmentService.complete(id).subscribe({
      next: () => this.loadAppointments(),
    });
  }

  getStatusLabel(status: AppointmentStatus): string {
    const map: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]: 'Bekliyor',
      [AppointmentStatus.Confirmed]: 'Onaylandı',
      [AppointmentStatus.Completed]: 'Tamamlandı',
      [AppointmentStatus.Cancelled]: 'İptal',
      [AppointmentStatus.NoShow]: 'Gelmedi',
    };
    return map[status] ?? '—';
  }

  getStatusClass(status: AppointmentStatus): string {
    const map: Record<AppointmentStatus, string> = {
      [AppointmentStatus.Pending]: 'badge-warning',
      [AppointmentStatus.Confirmed]: 'badge-info',
      [AppointmentStatus.Completed]: 'badge-success',
      [AppointmentStatus.Cancelled]: 'badge-danger',
      [AppointmentStatus.NoShow]: 'badge-gray',
    };
    return map[status] ?? 'badge-gray';
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
      timeZone: 'Europe/Istanbul'
    });
  }
  formatSlotTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  formatSlotValue(dateStr: string): string {
    return new Date(dateStr).toTimeString().slice(0, 5);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }
  get staffOptions(): SelectOption[] {
    return this.staffList.map((s) => ({ value: s.id, label: s.fullName }));
  }

  get customerOptions(): SelectOption[] {
    return this.customerList.map((c) => ({
      value: c.id,
      label: `${c.fullName} — ${c.phone}`,
    }));
  }

  get serviceOptions(): SelectOption[] {
    return this.serviceList.map((s) => ({
      value: s.id,
      label: `${s.name} — ${s.durationMinutes} dk`,
    }));
  }
  confirmAppointment(id: string): void {
  this.appointmentService.confirm(id).subscribe({
    next: () => this.loadAppointments(),
    error: (err) => console.error('Confirm error:', err)
  });
}
  onSelectChange(field: string, value: string): void {
    this.appointmentForm.patchValue({ [field]: value });
    if (field === 'staffId' || field === 'serviceId') {
      this.onFormFieldChange();
    }
  }
}
