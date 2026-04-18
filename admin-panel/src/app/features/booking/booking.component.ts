import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import {
  BookingApiService,
  SalonInfo,
  BookingService,
  BookingStaff,
  BookingSlot
} from '../../core/services/booking.service';
import { CustomCalendarComponent } from '../../shared/components/custom-calendar/custom-calendar.component';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, CustomCalendarComponent],
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.scss'
})
export class BookingComponent implements OnInit {
  subdomain    = '';
  salon:       SalonInfo | null = null;
  services:    BookingService[] = [];
  staffList:   BookingStaff[]   = [];
  slots:       BookingSlot[]    = [];

  selectedService: BookingService | null = null;
  selectedStaff:   BookingStaff   | null = null;
  selectedDate     = '';
  selectedSlot     = '';

  currentStep  = 1;
  isLoading    = false;
  isSubmitting = false;
  isSuccess    = false;
  errorMessage = '';
  bookingResult: any = null;

  customerForm: FormGroup;

  get minDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  get maxDate(): string {
    const date = new Date();
    date.setDate(date.getDate() + 15);
    return date.toISOString().split('T')[0];
  }

  constructor(
    private route:          ActivatedRoute,
    private bookingService: BookingApiService,
    private fb:             FormBuilder
  ) {
    this.customerForm = this.fb.group({
      fullName: ['', [Validators.required]],
      phone:    ['', [Validators.required]],
      email:    ['', [Validators.email]],
      notes:    ['']
    });
  }

  ngOnInit(): void {
    this.subdomain = this.route.snapshot.paramMap.get('subdomain') || '';
    this.loadSalon();
  }

  loadSalon(): void {
    this.isLoading = true;
    this.bookingService.getSalon(this.subdomain).subscribe({
      next: (res) => {
        if (res.success) {
          this.salon = res.data;
          this.loadServices();
          this.loadStaff();
        }
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Salon bulunamadı.';
        this.isLoading    = false;
      }
    });
  }

  loadServices(): void {
    this.bookingService.getServices(this.subdomain).subscribe({
      next: (res) => { if (res.success) this.services = res.data; }
    });
  }

  loadStaff(): void {
    this.bookingService.getStaff(this.subdomain).subscribe({
      next: (res) => { if (res.success) this.staffList = res.data; }
    });
  }

  selectService(service: BookingService): void {
    this.selectedService = service;
    this.selectedSlot    = '';
    this.slots           = [];
    this.loadSlotsIfReady();
  }

  selectStaff(staff: BookingStaff): void {
    this.selectedStaff = staff;
    this.selectedSlot  = '';
    this.slots         = [];
    this.loadSlotsIfReady();
  }

  onDateChange(date: string): void {
    this.selectedDate = date;
    this.selectedSlot = '';
    this.slots        = [];
    this.loadSlotsIfReady();
  }

  loadSlotsIfReady(): void {
    if (!this.selectedService || !this.selectedStaff || !this.selectedDate) return;

    this.bookingService.getAvailableSlots(
      this.subdomain,
      this.selectedStaff.id,
      this.selectedService.id,
      new Date(this.selectedDate).toISOString()
    ).subscribe({
      next: (res) => {
        if (res.success)
          this.slots = res.data.filter((s: BookingSlot) => s.isAvailable);
      }
    });
  }

  selectSlot(slot: BookingSlot): void {
    this.selectedSlot = slot.startTime;
  }

  canProceed(): boolean {
    return !!this.selectedService &&
           !!this.selectedStaff   &&
           !!this.selectedDate    &&
           !!this.selectedSlot;
  }

  onSubmit(): void {
    if (this.customerForm.invalid || !this.canProceed()) return;

    this.isSubmitting = true;
    this.errorMessage = '';

    const { fullName, phone, email, notes } = this.customerForm.value;

    this.bookingService.createAppointment(this.subdomain, {
      fullName,
      phone,
      email,
      staffId:   this.selectedStaff!.id,
      serviceId: this.selectedService!.id,
      startTime: this.selectedSlot,
      notes
    }).subscribe({
      next: (res) => {
        if (res.success) {
          this.isSuccess    = true;
          this.bookingResult = res.data;
        } else {
          this.errorMessage = res.message;
        }
        this.isSubmitting = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Randevu oluşturulamadı.';
        this.isSubmitting = false;
      }
    });
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit', minute: '2-digit'
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR', {
      weekday: 'long', day: 'numeric', month: 'long'
    });
  }

  formatPrice(price: number, currency: string): string {
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency', currency: currency || 'TRY'
    }).format(price);
  }

  getInitials(name: string): string {
    return name.split(' ').map(n => n[0]).join('').toUpperCase().slice(0, 2);
  }
}