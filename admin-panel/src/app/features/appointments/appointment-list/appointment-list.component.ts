import { Component, HostListener, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormsModule,
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
    FormsModule,
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

  // ─── Müşteri arama / otomatik doldurma ────────────────────
  customerQuery = '';
  showCustomerDropdown = false;
  selectedCustomer: Customer | null = null;

  // Türkiye yerel tarihi — toISOString() UTC verdiği için gece 00:00-03:00 arası
  // listeyi düne sabitliyor, takvim ise bugünü işaretliyordu.
  selectedDate = new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' });
  selectedStaffId = '';
  showPendingAll = false;

  viewMode: 'list' | 'calendar' = 'list';

  // ── Aylık takvim ──────────────────────────────────────────
  calendarYear  = new Date().getFullYear();
  calendarMonth = new Date().getMonth();
  monthAppointments: Appointment[] = [];
  selectedCalendarDate: string | null = null;

  AppointmentStatus = AppointmentStatus;

  // ─── Çoklu hizmet seçimi ──────────────────────────────────
  selectedServiceIds: Set<string> = new Set();

  // Personele özel fiyat/süre overrides: serviceId → override
  staffServiceOverrides = new Map<string, { customPrice: number | null; customCurrency: string | null; customDurationMinutes: number | null }>();

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
      customerId:  ['', Validators.required],
      staffId:     ['', Validators.required],
      servicesValid: [false, Validators.requiredTrue],  // en az 1 hizmet seçili mi
      date:        ['', Validators.required],
      startTime:   ['', Validators.required],
      notes:       [''],
    });
    this.editForm = this.fb.group({
      staffId:   ['', Validators.required],
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
    if (!target.closest('.customer-lookup')) {
      this.showCustomerDropdown = false;
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

  // Türkiye yerel tarihini kullan — toISOString() UTC'ye çevirdiği için
  // gece yarısı 00:00-03:00 arası bir önceki günü "bugün" olarak gösterip
  // geçmiş tarihe randevu seçilmesine izin verebiliyordu.
  get minDate(): string { return new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' }); }

  get maxDate(): string {
    const date = new Date();
    date.setDate(date.getDate() + 15);
    return date.toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' });
  }

  get visibleAppointments() {
    return this.showPendingAll
      ? this.appointments.filter(a => a.status === AppointmentStatus.Pending)
      : this.appointments;
  }

  /**
   * Başlıktaki sayaç. Liste ve takvim farklı günlere bakabildiği için
   * (selectedDate vs selectedCalendarDate) sayacı aktif görünüme göre üret —
   * aksi halde takvimde başka bir güne tıklandığında başlık ile gün paneli
   * farklı sayı gösteriyordu.
   */
  get headerCount(): number {
    return this.viewMode === 'calendar'
      ? this.selectedDayApts.length
      : this.visibleAppointments.length;
  }

  togglePendingAll(): void {
    this.showPendingAll = !this.showPendingAll;
    this.loadAppointments();
  }

  loadAppointments(): void {
    this.isLoading = true;
    let dateStr: string | undefined;
    let statusStr: string | undefined;
    if (this.showPendingAll) {
      statusStr = 'Pending';
    } else if (this.selectedDate) {
      const [year, month, day] = this.selectedDate.split('-').map(Number);
      dateStr = new Date(Date.UTC(year, month - 1, day, 0, 0, 0)).toISOString();
    }
    this.appointmentService.getAll(this.selectedStaffId || undefined, dateStr, undefined, statusStr).subscribe({
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

  /**
   * Randevu değiştiren her işlemden sonra çağrılır.
   * Takvim görünümü ayrı bir veri kaynağından (monthAppointments) beslendiği için
   * sadece listeyi tazelemek takvimde eski durumu (ör. tamamlanmış randevuda hâlâ
   * "Tamamla" butonu) bırakıyordu — aktif görünüme göre ikisini de tazeliyoruz.
   */
  private refreshViews(): void {
    this.loadAppointments();
    if (this.viewMode === 'calendar') this.loadMonthAppointments();
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

  /** Takvim verisi tazelenirken eski sonuçların sızmaması için istemci tarafında da süz. */
  private matchesStaffFilter(apt: Appointment): boolean {
    return !this.selectedStaffId || apt.staffId === this.selectedStaffId;
  }

  getDayApts(day: number): Appointment[] {
    const key = this.getDayStr(day);
    return this.monthAppointments.filter(a => this.aptDate(a) === key && this.matchesStaffFilter(a));
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
    return this.monthAppointments.filter(
      a => this.aptDate(a) === this.selectedCalendarDate && this.matchesStaffFilter(a));
  }

  /**
   * Personel filtresi. app-custom-select değeri doğrudan yayınlar (DOM Event değil).
   * Filtre hem listeyi hem takvimi kapsadığı için her iki veri kaynağı da tazelenir.
   */
  onStaffFilter(staffId: string): void {
    this.selectedStaffId = staffId;
    this.refreshViews();
  }

  openDrawer(): void {
    this.errorMessage = '';
    this.availableSlots = [];
    this.selectedServiceIds = new Set();
    this.staffServiceOverrides.clear();
    this.appointmentForm.reset({ date: this.selectedDate, servicesValid: false });
    this.clearCustomerSelection();
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen = false;
    this.availableSlots = [];
    this.selectedServiceIds = new Set();
    this.staffServiceOverrides.clear();
    this.appointmentForm.reset();
    this.clearCustomerSelection();
  }

  toggleServiceSelection(id: string): void {
    if (this.selectedServiceIds.has(id)) {
      this.selectedServiceIds.delete(id);
    } else {
      this.selectedServiceIds.add(id);
    }
    this.appointmentForm.patchValue({ servicesValid: this.selectedServiceIds.size > 0 });
    this.onFormFieldChange();
  }

  get filteredCustomerSuggestions(): Customer[] {
    const q = this.customerQuery.trim().toLowerCase();
    if (!q) return [];
    return this.customerList
      .filter(c => c.phone.includes(q) || c.fullName.toLowerCase().includes(q))
      .slice(0, 6);
  }

  onCustomerQueryChange(): void {
    this.selectedCustomer = null;
    this.appointmentForm.patchValue({ customerId: '' });
    this.showCustomerDropdown = true;
  }

  selectCustomerSuggestion(customer: Customer): void {
    this.selectedCustomer = customer;
    this.customerQuery = customer.phone;
    this.appointmentForm.patchValue({ customerId: customer.id });
    this.showCustomerDropdown = false;
  }

  clearCustomerSelection(): void {
    this.selectedCustomer = null;
    this.customerQuery = '';
    this.showCustomerDropdown = false;
    this.appointmentForm.patchValue({ customerId: '' });
  }

  onFormFieldChange(): void {
    const { staffId, date } = this.appointmentForm.value;
    const firstServiceId = [...this.selectedServiceIds][0];
    if (staffId && firstServiceId && date) {
      this.loadAvailableSlots(staffId, firstServiceId, date);
    }
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
    this.appointmentForm.markAllAsTouched();
    if (this.appointmentForm.invalid || this.selectedServiceIds.size === 0) return;
    this.isSubmitting = true;
    this.errorMessage = '';
    const { customerId, staffId, date, startTime, notes } = this.appointmentForm.value;
    const utcStartTime = new Date(`${date}T${startTime}:00+03:00`).toISOString();
    this.appointmentService.create({
      customerId,
      staffId,
      serviceIds: [...this.selectedServiceIds],
      startTime: utcStartTime,
      notes,
    }).subscribe({
      next: (res) => {
        if (res.success) { this.refreshViews(); this.closeDrawer(); }
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
        if (res.success) { this.refreshViews(); this.closeEditModal(); }
        this.isEditSubmitting = false;
      },
      error: (err) => { this.editError = err.error?.message || 'Hata oluştu.'; this.isEditSubmitting = false; }
    });
  }

  cancelAppointment(id: string): void {
    if (!confirm('?')) return;
    this.appointmentService.cancel(id).subscribe({ next: () => this.refreshViews() });
  }

  // ─── TAMAMLA MODAL ────────────────────────────────────────
  completingAppointment: Appointment | null = null;
  completeSelectedServiceIds: Set<string> = new Set();
  completeActualPrice: number | null = null;
  completeNotes = '';
  isCompleteSubmitting = false;
  completeError = '';
  completeReceiptNumber = '';

  openCompleteModal(apt: Appointment): void {
    this.completingAppointment = apt;
    this.completeSelectedServiceIds = new Set([apt.serviceId]);
    this.completeActualPrice = apt.price ?? null;
    this.completeNotes = '';
    this.completeError = '';
    this.completeReceiptNumber = '';
    this.isCompleteSubmitting = false;
  }

  closeCompleteModal(): void {
    this.completingAppointment = null;
    this.completeReceiptNumber = '';
  }

  toggleCompleteService(id: string): void {
    if (this.completeSelectedServiceIds.has(id)) {
      this.completeSelectedServiceIds.delete(id);
    } else {
      this.completeSelectedServiceIds.add(id);
    }
    this.recalcActualPrice();
  }

  recalcActualPrice(): void {
    let total = 0;
    for (const svc of this.serviceList) {
      if (this.completeSelectedServiceIds.has(svc.id)) {
        total += svc.price ?? 0;
      }
    }
    this.completeActualPrice = total > 0 ? total : null;
  }

  submitComplete(): void {
    if (!this.completingAppointment) return;
    this.isCompleteSubmitting = true;
    this.completeError = '';
    this.appointmentService.complete(this.completingAppointment.id, {
      actualServiceIds: Array.from(this.completeSelectedServiceIds),
      actualTotalPrice: this.completeActualPrice,
      completionNotes: this.completeNotes || null,
    }).subscribe({
      next: (res) => {
        this.refreshViews();
        if (res?.data?.receiptNumber) {
          this.completeReceiptNumber = res.data.receiptNumber;
          this.isCompleteSubmitting = false;
        } else {
          this.closeCompleteModal();
        }
      },
      error: (err) => {
        this.completeError = err.error?.message || err.error?.errors?.[0] || 'Randevu tamamlanamadı.';
        this.isCompleteSubmitting = false;
      }
    });
  }

  confirmAppointment(id: string): void {
    this.appointmentService.confirm(id).subscribe({
      next: () => this.refreshViews(),
      error: () => this.refreshViews(),
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

  getStatusIndex(status: AppointmentStatus): number {
    const map: Record<AppointmentStatus, number> = {
      [AppointmentStatus.Pending]:   0,
      [AppointmentStatus.Confirmed]: 1,
      [AppointmentStatus.Completed]: 2,
      [AppointmentStatus.Cancelled]: 3,
      [AppointmentStatus.NoShow]:    4,
    };
    return map[status] ?? 0;
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
    if (field === 'staffId') {
      this.loadStaffServiceOverrides(value);
      this.onFormFieldChange();
    }
  }

  loadStaffServiceOverrides(staffId: string): void {
    this.staffServiceOverrides.clear();
    if (!staffId) return;
    this.staffService.getServices(staffId).subscribe({
      next: (res: any) => {
        if (res.success && Array.isArray(res.data)) {
          for (const item of res.data) {
            this.staffServiceOverrides.set(item.serviceId, {
              customPrice: item.customPrice ?? null,
              customCurrency: item.customCurrency ?? null,
              customDurationMinutes: item.customDurationMinutes ?? null,
            });
          }
        }
      },
    });
  }

  effectivePrice(svc: Service): number {
    return this.staffServiceOverrides.get(svc.id)?.customPrice ?? svc.price;
  }

  effectiveCurrency(svc: Service): string {
    return this.staffServiceOverrides.get(svc.id)?.customCurrency ?? svc.currency;
  }

  effectiveDuration(svc: Service): number {
    return this.staffServiceOverrides.get(svc.id)?.customDurationMinutes ?? svc.durationMinutes;
  }

  get selectedServicesLabel(): string {
    const names = this.serviceList
      .filter(s => this.selectedServiceIds.has(s.id))
      .map(s => s.name);
    return names.length > 0 ? names.join(' + ') : '';
  }

  get selectedServicesTotalDuration(): number {
    return this.serviceList
      .filter(s => this.selectedServiceIds.has(s.id))
      .reduce((sum, s) => sum + this.effectiveDuration(s), 0);
  }
}