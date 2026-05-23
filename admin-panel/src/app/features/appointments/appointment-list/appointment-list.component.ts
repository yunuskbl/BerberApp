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

  // ── Aylık takvim ──────────────────────────────────────────
  calendarYear  = new Date().getFullYear();
  calendarMonth = new Date().getMonth();
  monthAppointments: Appointment[] = [];
  selectedCalendarDate: string | null = null;

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
    this.editForm = this.fb.group({
      staffId:   ['', Validators.required],
      serviceId: ['', Validators.required],
      date:      ['', Validators.required],
      startTime: ['', Validators.required],
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

  setViewMode(mode: 'list' | 'calendar'): void {
    this.viewMode = mode;
    if (mode === 'calendar') this.loadMonthAppointments();
  }

  loadMonthAppointments(): void {
    this.isLoading = true;
    this.appointmentService.getAll(this.selectedStaffId || undefined).subscribe({
      next: (res) => { if (res.success) this.monthAppointments = res.data; this.isLoading = false; },
      error: () => { this.isLoading = false; },
    });
  }

  prevCalMonth(): void {
    if (this.calendarMonth === 0) { this.calendarMonth = 11; this.calendarYear--; }
    else this.calendarMonth--;
    this.selectedCalendarDate = null;
  }

  nextCalMonth(): void {
    if (this.calendarMonth === 11) { this.calendarMonth = 0; this.calendarYear++; }
    else this.calendarMonth++;
    this.selectedCalendarDate = null;
  }

  get calendarDays(): (number | null)[] {
    const firstDay = new Date(this.calendarYear, this.calendarMonth, 1);
    let startOffset = firstDay.getDay() - 1;
    if (startOffset < 0) startOffset = 6;
    const lastDay = new Date(this.calendarYear, this.calendarMonth + 1, 0).getDate();
    const days: (number | null)[] = [];
    for (let i = 0; i < startOffset; i++) days.push(null);
    for (let i = 1; i <= lastDay; i++) days.push(i);
    return days;
  }

  get calendarWeekDays(): string[] {
    const lang = this.langService.lang();
    const map: Record<string, string[]> = {
      tr: ['Pzt', 'Sal', 'Çar', 'Per', 'Cum', 'Cmt', 'Paz'],
      en: ['Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'],
      ru: ['Пн',  'Вт',  'Ср',  'Чт',  'Пт',  'Сб',  'Вс' ],
    };
    return map[lang] ?? map['tr'];
  }

  get calendarMonthLabel(): string {
    return new Intl.DateTimeFormat(this.langService.dateLocale, { month: 'long', year: 'numeric' })
      .format(new Date(this.calendarYear, this.calendarMonth, 1));
  }

  private getDayStr(day: number): string {
    return `${this.calendarYear}-${String(this.calendarMonth + 1).padStart(2, '0')}-${String(day).padStart(2, '0')}`;
  }

  private aptDate(apt: Appointment): string {
    return new Date(apt.startTime).toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' });
  }

  getDayApts(day: number): Appointment[] {
    const key = this.getDayStr(day);
    return this.monthAppointments.filter(a => this.aptDate(a) === key);
  }

  selectCalDay(day: number | null): void {
    if (!day) return;
    this.selectedCalendarDate = this.getDayStr(day);
  }

  isCalToday(day: number | null): boolean {
    if (!day) return false;
    const t = new Date();
    return day === t.getDate() && this.calendarMonth === t.getMonth() && this.calendarYear === t.getFullYear();
  }

  isCalSelected(day: number | null): boolean {
    return !!day && this.selectedCalendarDate === this.getDayStr(day);
  }

  get selectedDayLabel(): string {
    if (!this.selectedCalendarDate) return '';
    return new Date(this.selectedCalendarDate + 'T12:00:00').toLocaleDateString(
      this.langService.dateLocale, { day: 'numeric', month: 'long', weekday: 'long' });
  }

  get selectedDayApts(): Appointment[] {
    if (!this.selectedCalendarDate) return [];
    return this.monthAppointments.filter(a => this.aptDate(a) === this.selectedCalendarDate);
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
        if (res.success) this.availableSlots = res.data;
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
    // startTime form değeri Türkiye yerel saatinde ("HH:mm"). UTC'ye çevirerek gönder.
    // Örn: "12:00" → "2026-05-09T12:00:00+03:00" → "2026-05-09T09:00:00.000Z"
    const utcStartTime = new Date(`${date}T${startTime}:00+03:00`).toISOString();
    this.appointmentService.create({ customerId, staffId, serviceId, startTime: utcStartTime, notes }).subscribe({
      next: (res) => {
        if (res.success) { this.loadAppointments(); this.closeDrawer(); }
        this.isSubmitting = false;
      },
      error: (err) => { this.errorMessage = err.error?.message || 'Hata oluştu.'; this.isSubmitting = false; }
    });
  }

  // ─── GÜNCELLE ─────────────────────────────────────────────
  editingAppointment: Appointment | null = null;
  editSlots:          AvailableSlot[]    = [];
  isEditSlotsLoading  = false;
  isEditSubmitting    = false;
  editError           = '';

  editForm!: FormGroup;

  openEditModal(apt: Appointment): void {
    this.editingAppointment = apt;
    this.editError          = '';
    this.editSlots          = [];
    const d = new Date(apt.startTime).toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' }); // YYYY-MM-DD
    this.editForm.patchValue({ staffId: apt.staffId, serviceId: apt.serviceId, date: d, startTime: apt.startTime });
    this.loadEditSlots();
  }

  closeEditModal(): void { this.editingAppointment = null; }

  onEditFieldChange(): void { this.loadEditSlots(); }

  loadEditSlots(): void {
    const { staffId, serviceId, date } = this.editForm.value;
    if (!staffId || !serviceId || !date) return;
    this.isEditSlotsLoading = true;
    this.appointmentService.getAvailableSlots(staffId, serviceId, date + 'T00:00:00Z').subscribe({
      next: (res) => {
        if (res.success) this.editSlots = res.data;
        this.isEditSlotsLoading = false;
      },
      error: () => { this.isEditSlotsLoading = false; }
    });
  }

  onEditSubmit(): void {
    if (this.editForm.invalid || !this.editingAppointment) return;
    this.isEditSubmitting = true;
    this.editError        = '';
    const { staffId, serviceId, startTime } = this.editForm.value;
    this.appointmentService.update(this.editingAppointment.id, { staffId, serviceId, startTime }).subscribe({
      next: (res) => {
        if (res.success) { this.loadAppointments(); this.closeEditModal(); }
        this.isEditSubmitting = false;
      },
      error: (err) => { this.editError = err.error?.message || 'Hata oluştu.'; this.isEditSubmitting = false; }
    });
  }

  cancelAppointment(id: string): void {
    if (!confirm('?')) return;
    this.appointmentService.cancel(id).subscribe({ next: () => this.loadAppointments() });
  }

  completeAppointment(id: string): void {
    this.errorMessage = '';
    this.appointmentService.complete(id).subscribe({
      next: () => this.loadAppointments(),
      error: (err) => {
        this.errorMessage = err.error?.message || err.error?.errors?.[0] || 'Randevu tamamlanamadı.';
      }
    });
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
      hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul',
    });
  }

  formatSlotValue(dateStr: string): string {
    // Form değeri olarak Türkiye saatini döndür (HH:mm) — makine timezone'undan bağımsız
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'Europe/Istanbul',
    }).slice(0, 5);
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