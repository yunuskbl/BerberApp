import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router } from '@angular/router';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  FormsModule,
} from '@angular/forms';
import {
  BookingApiService,
  SalonInfo,
  BookingService,
  BookingStaff,
  BookingSlot,
} from '../../core/services/booking.service';
import { CustomCalendarComponent } from '../../shared/components/custom-calendar/custom-calendar.component';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomCalendarComponent,
    FormsModule,
  ],
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.scss',
})
export class BookingComponent implements OnInit {
  subdomain = '';
  salon: SalonInfo | null = null;
  services: BookingService[] = [];
  staffList: BookingStaff[] = [];
  slots: BookingSlot[] = [];

  selectedService: BookingService | null = null;
  selectedStaff: BookingStaff | null = null;
  selectedDate = '';
  selectedSlot = '';

  currentStep = 1;
  isLoading = false;
  isSubmitting = false;
  isSuccess = false;
  errorMessage = '';
  bookingResult: any = null;

  otpSent = false;
  otpVerified = true;
  otpCode = '';
  isSendingOtp = false;
  isVerifyingOtp = false;
  otpError = '';
  otpSuccess = '';
  otpTimer = 0;
  private timerInterval: any;

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
    private route: ActivatedRoute,
    private bookingService: BookingApiService,
    private fb: FormBuilder,
    private titleService: Title,
    private router: Router,
  ) {
    this.customerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(30)]],
      phone: [
        '',
        [
          Validators.required,
          Validators.maxLength(11),
          Validators.pattern(/^[0-9\s\-\+\(\)]{10,15}$/),
        ],
      ],
      email: ['', [Validators.email, Validators.maxLength(80)]],
      notes: ['', [Validators.maxLength(200)]],
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
          this.titleService.setTitle(this.salon!.name + ' - BerberApp');
          this.loadServices();
          this.loadStaff();
        }
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Salon bulunamadı.';
        this.isLoading = false;
      },
    });
  }

  loadServices(): void {
    this.bookingService.getServices(this.subdomain).subscribe({
      next: (res) => {
        if (res.success) this.services = res.data;
      },
    });
  }

  loadStaff(): void {
    this.bookingService.getStaff(this.subdomain).subscribe({
      next: (res) => {
        if (res.success) this.staffList = res.data;
      },
    });
  }

  selectService(service: BookingService): void {
    this.selectedService = service;
    this.selectedSlot = '';
    this.slots = [];
    this.loadSlotsIfReady();
  }

  selectStaff(staff: BookingStaff): void {
    this.selectedStaff = staff;
    this.selectedSlot = '';
    this.slots = [];
    this.loadSlotsIfReady();
  }

  onDateChange(date: string): void {
    this.selectedDate = date;
    this.selectedSlot = '';
    this.slots = [];
    this.loadSlotsIfReady();
  }

  loadSlotsIfReady(): void {
    if (!this.selectedService || !this.selectedStaff || !this.selectedDate)
      return;

    // "2025-04-23" → timezone kayması olmadan UTC'ye gönder
    const dateUtc = this.selectedDate + 'T00:00:00Z';

    this.bookingService
      .getAvailableSlots(
        this.subdomain,
        this.selectedStaff.id,
        this.selectedService.id,
        dateUtc, // <-- değişti
      )
      .subscribe({
        next: (res) => {
          if (res.success)
            this.slots = res.data.filter((s: BookingSlot) => s.isAvailable);
        },
      });
  }

  selectSlot(slot: BookingSlot): void {
    this.selectedSlot = slot.startTime;
  }

  canProceed(): boolean {
    return (
      !!this.selectedService &&
      !!this.selectedStaff &&
      !!this.selectedDate &&
      !!this.selectedSlot
    );
  }

  onSubmit(): void {
    if (this.customerForm.invalid || !this.canProceed()) return;

    this.isSubmitting = true;
    this.errorMessage = '';

    const { fullName, phone, email, notes } = this.customerForm.value;

    this.bookingService
      .createAppointment(this.subdomain, {
        fullName,
        phone,
        email,
        staffId: this.selectedStaff!.id,
        serviceId: this.selectedService!.id,
        startTime: this.selectedSlot,
        notes,
      })
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.router.navigate([
              '/randevu',
              this.subdomain,
              res.appointmentId,
            ]);
          } else {
            this.errorMessage = res.message;
          }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Randevu oluşturulamadı.';
          this.isSubmitting = false;
        },
      });
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit',
      minute: '2-digit',
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR', {
      weekday: 'long',
      day: 'numeric',
      month: 'long',
    });
  }

  formatPrice(price: number, currency: string): string {
    if (!price || price <= 0) return 'Belirtilmemiş';
    return new Intl.NumberFormat('tr-TR', {
      style: 'currency',
      currency: currency || 'TRY',
    }).format(price);
  }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  // sendOtp(): void {
  //   const phone = this.customerForm.get('phone')?.value;
  //   if (!phone) return;

  //   this.isSendingOtp = true;
  //   this.otpError = '';

  //   this.bookingService.sendOtp(phone).subscribe({
  //     next: (res) => {
  //       if (res.success) {
  //         this.otpSent = true;
  //         this.otpSuccess = 'Doğrulama kodu Sms gönderildi!';
  //         this.startTimer();
  //       }
  //       this.isSendingOtp = false;
  //     },
  //     error: (err) => {
  //       this.otpError = err.error?.message || 'Kod gönderilemedi.';
  //       this.isSendingOtp = false;
  //     },
  //   });
  // }

  // verifyOtp(): void {
  //   const phone = this.customerForm.get('phone')?.value;
  //   if (!phone || !this.otpCode) return;

  //   this.isVerifyingOtp = true;
  //   this.otpError = '';

  //   this.bookingService.verifyOtp(phone, this.otpCode).subscribe({
  //     next: (res) => {
  //       if (res.success) {
  //         this.otpVerified = true;
  //         this.otpSuccess = 'Telefon doğrulandı! ✓';
  //       }
  //       this.isVerifyingOtp = false;
  //     },
  //     error: (err) => {
  //       this.otpError = err.error?.message || 'Hatalı kod.';
  //       this.isVerifyingOtp = false;
  //     },
  //   });
  // }

  startTimer(): void {
    this.otpTimer = 120; // 2 dakika
    this.timerInterval = setInterval(() => {
      this.otpTimer--;
      if (this.otpTimer <= 0) {
        clearInterval(this.timerInterval);
        this.otpSent = false;
      }
    }, 1000);
  }

  get timerDisplay(): string {
    const m = Math.floor(this.otpTimer / 60);
    const s = this.otpTimer % 60;
    return `${m}:${s.toString().padStart(2, '0')}`;
  }
}
