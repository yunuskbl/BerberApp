import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators, FormsModule } from '@angular/forms';
import { CustomerService } from '../../../core/services/customer.service';
import { Customer } from '../../../core/models/customer.model';
import { AppointmentService } from '../../../core/services/appointment.service';
import { Appointment, AppointmentStatus, AvailableSlot } from '../../../core/models/appointment.model';
import { StaffService } from '../../../core/services/staff.service';
import { ServiceService } from '../../../core/services/service.service';
import { Staff } from '../../../core/models/staff.model';
import { Service } from '../../../core/models/service.model';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

@Component({
  selector: 'app-customer-list',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, TranslatePipe],
  templateUrl: './customer-list.component.html',
  styleUrl: './customer-list.component.scss'
})
export class CustomerListComponent implements OnInit {
  customerList:   Customer[]    = [];
  filteredList:   Customer[]    = [];
  staffList:      Staff[]       = [];
  serviceList:    Service[]     = [];
  isLoading       = true;
  isDrawerOpen    = false;
  isSubmitting    = false;
  editingCustomer: Customer | null = null;
  errorMessage    = '';
  searchQuery     = '';

  customerForm: FormGroup;

  // --- Geçmiş ---
  historyCustomer:    Customer | null = null;
  customerHistory:    Appointment[]   = [];
  isHistoryOpen       = false;
  isHistoryLoading    = false;

  // --- Güncelle modal ---
  updatingAppointment: Appointment | null = null;
  updateSlots:         AvailableSlot[]    = [];
  updateSlotsLoading   = false;
  updateSubmitting     = false;
  updateError          = '';
  updateForm: FormGroup;

  // --- Toplu mesaj ---
  isBroadcastOpen      = false;
  broadcastMessage     = '';
  broadcastSubmitting  = false;
  broadcastResult: { totalCustomers: number; filteredCount: number; sent: number; failed: number } | null = null;
  broadcastError       = '';
  broadcastFilter: 'All' | 'NotVisitedSince' | 'NeverVisited' | 'FrequentCustomers' | 'RecentVisitors' = 'All';
  broadcastFilterDays   = 30;
  broadcastMinAppts     = 3;
  broadcastDropdownOpen = false;
  broadcastImageUrl     = '';
  broadcastImageUploading = false;
  broadcastImageError   = '';

  readonly broadcastFilterOptions = [
    { value: 'All',               label: 'Tüm müşteriler' },
    { value: 'NotVisitedSince',   label: 'Son X gün gelmeyenler' },
    { value: 'NeverVisited',      label: 'Hiç gelmeyenler' },
    { value: 'FrequentCustomers', label: 'En az X randevusu olanlar' },
    { value: 'RecentVisitors',    label: 'Son X günde gelenler' },
  ] as const;

  get broadcastFilterLabel(): string {
    const opt = this.broadcastFilterOptions.find(o => o.value === this.broadcastFilter);
    if (this.broadcastFilter === 'All') return `Tüm müşteriler (${this.customerList.length} kişi)`;
    return opt?.label ?? '';
  }

  selectBroadcastFilter(value: typeof this.broadcastFilter): void {
    this.broadcastFilter = value;
    this.broadcastDropdownOpen = false;
  }

  // --- Tekrarla modal ---
  repeatingAppointment: Appointment | null = null;
  repeatDate   = '';
  repeatSlots:  AvailableSlot[] = [];
  repeatSlot   = '';
  repeatSlotsLoading = false;
  repeatSubmitting   = false;
  repeatError        = '';

  AppointmentStatus = AppointmentStatus;

  constructor(
    private customerService:    CustomerService,
    private appointmentService: AppointmentService,
    private staffService:       StaffService,
    private serviceService:     ServiceService,
    private fb: FormBuilder,
    public langService: LanguageService,
  ) {
    this.customerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(100)]],
      phone:    ['', [Validators.required]],
      email:    ['', [Validators.email]],
      notes:    ['']
    });

    this.updateForm = this.fb.group({
      staffId:   ['', Validators.required],
      serviceId: ['', Validators.required],
      date:      ['', Validators.required],
      startTime: ['', Validators.required],
    });
  }

  ngOnInit(): void {
    this.loadCustomers();
    this.staffService.getAll().subscribe(r => { if (r.success) this.staffList = r.data; });
    this.serviceService.getAll().subscribe(r => { if (r.success) this.serviceList = r.data; });
  }

  loadCustomers(): void {
    this.isLoading = true;
    this.customerService.getAll().subscribe({
      next: (res) => {
        if (res.success) { this.customerList = res.data; this.filteredList = res.data; }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; }
    });
  }

  onSearch(event: Event): void {
    const query = (event.target as HTMLInputElement).value.toLowerCase();
    this.searchQuery  = query;
    this.filteredList = this.customerList.filter(c =>
      c.fullName.toLowerCase().includes(query) ||
      c.phone.includes(query) ||
      (c.email?.toLowerCase().includes(query) ?? false)
    );
  }

  openDrawer(customer?: Customer): void {
    this.editingCustomer = customer || null;
    this.errorMessage    = '';
    if (customer) {
      this.customerForm.patchValue(customer);
    } else {
      this.customerForm.reset();
    }
    this.isDrawerOpen = true;
  }

  closeDrawer(): void {
    this.isDrawerOpen    = false;
    this.editingCustomer = null;
    this.customerForm.reset();
  }

  onSubmit(): void {
    if (this.customerForm.invalid) return;
    this.isSubmitting = true;
    this.errorMessage = '';
    const value = this.customerForm.value;

    if (this.editingCustomer) {
      this.customerService.update(this.editingCustomer.id, value).subscribe({
        next: (res) => {
          if (res.success) { this.loadCustomers(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => { this.errorMessage = err.error?.message || 'Hata oluştu.'; this.isSubmitting = false; }
      });
    } else {
      this.customerService.create(value).subscribe({
        next: (res) => {
          if (res.success) { this.loadCustomers(); this.closeDrawer(); }
          this.isSubmitting = false;
        },
        error: (err) => { this.errorMessage = err.error?.message || 'Hata oluştu.'; this.isSubmitting = false; }
      });
    }
  }

  deleteCustomer(id: string): void {
    if (!confirm('?')) return;
    this.customerService.delete(id).subscribe({ next: () => this.loadCustomers() });
  }

  // ─── GEÇMİŞ ─────────────────────────────────────────────
  openHistory(customer: Customer): void {
    this.historyCustomer  = customer;
    this.isHistoryOpen    = true;
    this.isHistoryLoading = true;
    this.customerHistory  = [];
    this.appointmentService.getByCustomer(customer.id).subscribe({
      next: (res) => { if (res.success) this.customerHistory = res.data; this.isHistoryLoading = false; },
      error: () => { this.isHistoryLoading = false; }
    });
  }

  closeHistory(): void {
    this.isHistoryOpen        = false;
    this.historyCustomer      = null;
    this.customerHistory      = [];
    this.updatingAppointment  = null;
    this.repeatingAppointment = null;
  }

  // ─── GÜNCELLE ────────────────────────────────────────────
  openUpdateModal(apt: Appointment): void {
    this.updatingAppointment  = apt;
    this.repeatingAppointment = null;
    this.updateError          = '';
    this.updateSlots          = [];
    const d = new Date(apt.startTime).toISOString().split('T')[0];
    this.updateForm.patchValue({ staffId: apt.staffId, serviceId: apt.serviceId, date: d, startTime: apt.startTime });
    this.loadUpdateSlots();
  }

  closeUpdateModal(): void { this.updatingAppointment = null; }

  onUpdateFormChange(): void { this.loadUpdateSlots(); }

  loadUpdateSlots(): void {
    const { staffId, serviceId, date } = this.updateForm.value;
    if (!staffId || !serviceId || !date) return;
    this.updateSlotsLoading = true;
    this.appointmentService.getAvailableSlots(staffId, serviceId, date + 'T00:00:00Z').subscribe({
      next: (res) => {
        if (res.success) this.updateSlots = res.data.filter(s => s.isAvailable);
        this.updateSlotsLoading = false;
      },
      error: () => { this.updateSlotsLoading = false; }
    });
  }

  onUpdateSubmit(): void {
    if (this.updateForm.invalid || !this.updatingAppointment) return;
    this.updateSubmitting = true;
    this.updateError      = '';
    const { staffId, serviceId, startTime } = this.updateForm.value;
    this.appointmentService.update(this.updatingAppointment.id, { staffId, serviceId, startTime }).subscribe({
      next: (res) => {
        if (res.success) {
          const idx = this.customerHistory.findIndex(a => a.id === this.updatingAppointment!.id);
          if (idx >= 0) this.customerHistory[idx] = res.data;
          this.closeUpdateModal();
        }
        this.updateSubmitting = false;
      },
      error: (err) => { this.updateError = err.error?.message || 'Hata oluştu.'; this.updateSubmitting = false; }
    });
  }

  // ─── TEKRARLA ────────────────────────────────────────────
  openRepeatModal(apt: Appointment): void {
    this.repeatingAppointment = apt;
    this.updatingAppointment  = null;
    this.repeatDate           = '';
    this.repeatSlot           = '';
    this.repeatSlots          = [];
    this.repeatError          = '';
  }

  closeRepeatModal(): void { this.repeatingAppointment = null; }

  onRepeatDateChange(): void {
    if (!this.repeatDate || !this.repeatingAppointment) return;
    this.repeatSlotsLoading = true;
    this.repeatSlot         = '';
    this.appointmentService
      .getAvailableSlots(this.repeatingAppointment.staffId, this.repeatingAppointment.serviceId, this.repeatDate + 'T00:00:00Z')
      .subscribe({
        next: (res) => { if (res.success) this.repeatSlots = res.data.filter(s => s.isAvailable); this.repeatSlotsLoading = false; },
        error: () => { this.repeatSlotsLoading = false; }
      });
  }

  onRepeatSubmit(): void {
    if (!this.repeatingAppointment || !this.repeatSlot || !this.historyCustomer) return;
    this.repeatSubmitting = true;
    this.repeatError      = '';
    this.appointmentService.create({
      customerId: this.historyCustomer.id,
      staffId:    this.repeatingAppointment.staffId,
      serviceIds: [this.repeatingAppointment.serviceId],
      startTime:  this.repeatSlot,
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.customerHistory.unshift(res.data);
          this.closeRepeatModal();
        }
        this.repeatSubmitting = false;
      },
      error: (err) => { this.repeatError = err.error?.message || 'Hata oluştu.'; this.repeatSubmitting = false; }
    });
  }

  // ─── TOPLU MESAJ ─────────────────────────────────────────
  openBroadcast(): void {
    this.isBroadcastOpen     = true;
    this.broadcastMessage    = '';
    this.broadcastResult     = null;
    this.broadcastError      = '';
    this.broadcastFilter          = 'All';
    this.broadcastFilterDays      = 30;
    this.broadcastMinAppts        = 3;
    this.broadcastDropdownOpen    = false;
    this.broadcastImageUrl        = '';
    this.broadcastImageUploading  = false;
    this.broadcastImageError      = '';
  }

  closeBroadcast(): void {
    this.isBroadcastOpen = false;
  }

  onBroadcastSubmit(): void {
    if (!this.broadcastMessage.trim() || this.broadcastSubmitting) return;
    this.broadcastSubmitting = true;
    this.broadcastResult = null;
    this.broadcastError = '';
    this.customerService.broadcast({
      message:         this.broadcastMessage.trim(),
      imageUrl:        this.broadcastImageUrl || undefined,
      filter:          this.broadcastFilter,
      filterDays:      ['NotVisitedSince', 'RecentVisitors'].includes(this.broadcastFilter) ? this.broadcastFilterDays : undefined,
      minAppointments: this.broadcastFilter === 'FrequentCustomers' ? this.broadcastMinAppts : undefined,
    }).subscribe({
      next: (res) => {
        if (res.success) this.broadcastResult = res.data;
        else this.broadcastError = 'Gönderim başarısız.';
        this.broadcastSubmitting = false;
      },
      error: (err) => {
        this.broadcastError = err.error?.message || 'Hata oluştu.';
        this.broadcastSubmitting = false;
      }
    });
  }

  onBroadcastImageSelect(event: Event): void {
    const file = (event.target as HTMLInputElement).files?.[0];
    if (!file) return;
    this.broadcastImageUploading = true;
    this.broadcastImageError = '';
    this.customerService.uploadBroadcastImage(file).subscribe({
      next: (res) => {
        if (res.success) this.broadcastImageUrl = res.data.url;
        else this.broadcastImageError = 'Görsel yüklenemedi.';
        this.broadcastImageUploading = false;
      },
      error: () => { this.broadcastImageError = 'Görsel yüklenemedi.'; this.broadcastImageUploading = false; }
    });
  }

  removeBroadcastImage(): void {
    this.broadcastImageUrl = '';
    this.broadcastImageError = '';
  }

  // ─── HELPERS ─────────────────────────────────────────────
  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleString('tr-TR', {
      day: '2-digit', month: 'short', year: 'numeric',
      hour: '2-digit', minute: '2-digit',
      timeZone: 'Europe/Istanbul',
    });
  }

  formatSlotTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul',
    });
  }

  statusLabel(status: AppointmentStatus): string {
    const map: Record<string, string> = {
      [AppointmentStatus.Pending]: 'Bekliyor', [AppointmentStatus.Confirmed]: 'Onaylandı',
      [AppointmentStatus.Completed]: 'Tamamlandı', [AppointmentStatus.Cancelled]: 'İptal',
      [AppointmentStatus.NoShow]: 'Gelmedi'
    };
    return map[status] ?? '—';
  }

  statusClass(status: AppointmentStatus): string {
    const map: Record<string, string> = {
      [AppointmentStatus.Pending]: 'pending', [AppointmentStatus.Confirmed]: 'confirmed',
      [AppointmentStatus.Completed]: 'completed', [AppointmentStatus.Cancelled]: 'cancelled',
      [AppointmentStatus.NoShow]: 'noshow'
    };
    return map[status] ?? '';
  }

  get minDate(): string { return new Date().toISOString().split('T')[0]; }
  get maxDate(): string {
    const d = new Date(); d.setDate(d.getDate() + 60);
    return d.toISOString().split('T')[0];
  }
}
