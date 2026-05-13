import { Component, OnInit, OnDestroy, ElementRef, Renderer2 } from '@angular/core';
import { Subject } from 'rxjs';
import { debounceTime, distinctUntilChanged, switchMap, takeUntil } from 'rxjs/operators';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
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
import { LanguageSwitcherComponent } from '../../shared/components/language-switcher/language-switcher.component';
import { LanguageService } from '../../core/services/language.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { DecorativeBgComponent } from '../../shared/components/decorative-bg/decorative-bg.component';

@Component({
  selector: 'app-booking',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    CustomCalendarComponent,
    FormsModule,
    LanguageSwitcherComponent,
    TranslatePipe,
    DecorativeBgComponent,
    RouterLink,
  ],
  templateUrl: './booking.component.html',
  styleUrl: './booking.component.scss',
})
export class BookingComponent implements OnInit, OnDestroy {
  subdomain = '';
  salon: SalonInfo | null = null;
  services: BookingService[] = [];
  staffList: BookingStaff[] = [];
  slots: BookingSlot[] = [];

  selectedStaff: BookingStaff | null = null;
  selectedServices: BookingService[] = [];
  selectedDate = '';
  selectedSlot = '';

  currentStep = 1;
  isLoading = false;
  isSubmitting = false;
  isSuccess = false;
  errorMessage = '';
  bookingResult: any = null;

  lightboxPhoto: string | null = null;
  lightboxIndex = 0;

  openLightbox(index: number): void {
    const photos = this.salon?.photos;
    if (!photos?.length) return;
    this.lightboxIndex = index;
    this.lightboxPhoto = photos[index].url;
  }

  closeLightbox(): void { this.lightboxPhoto = null; }

  lightboxNext(): void {
    const photos = this.salon?.photos ?? [];
    this.lightboxIndex = (this.lightboxIndex + 1) % photos.length;
    this.lightboxPhoto = photos[this.lightboxIndex].url;
  }

  lightboxPrev(): void {
    const photos = this.salon?.photos ?? [];
    this.lightboxIndex = (this.lightboxIndex - 1 + photos.length) % photos.length;
    this.lightboxPhoto = photos[this.lightboxIndex].url;
  }

  customerFound = false;
  isLookingUp = false;
  private phoneSubject$ = new Subject<string>();
  private destroy$ = new Subject<void>();

  otpSent = false;
  otpVerified = false;
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
    private el: ElementRef,
    private renderer: Renderer2,
    public langService: LanguageService,
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
    this.initPhoneLookup();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private initPhoneLookup(): void {
    this.phoneSubject$.pipe(
      debounceTime(600),
      distinctUntilChanged(),
      switchMap(phone => {
        const cleaned = phone.replace(/[\s\-\(\)\+]/g, '');
        if (cleaned.length < 10) {
          this.isLookingUp = false;
          this.customerFound = false;
          return [];
        }
        this.isLookingUp = true;
        return this.bookingService.lookupCustomer(this.subdomain, phone);
      }),
      takeUntil(this.destroy$),
    ).subscribe({
      next: (res: any) => {
        this.isLookingUp = false;
        if (res?.success && res.data) {
          this.customerFound = true;
          this.customerForm.patchValue({
            fullName: res.data.fullName || this.customerForm.get('fullName')?.value,
          });
        } else {
          this.customerFound = false;
        }
      },
      error: () => {
        this.isLookingUp = false;
        this.customerFound = false;
      },
    });
  }

  onPhoneInput(event: Event): void {
    const value = (event.target as HTMLInputElement).value;
    this.customerFound = false;
    this.phoneSubject$.next(value);
  }

  loadSalon(): void {
    this.isLoading = true;
    this.bookingService.getSalon(this.subdomain).subscribe({
      next: (res) => {
        if (res.success) {
          this.salon = res.data;
          this.titleService.setTitle(this.salon!.name + ' - BerberApp');
          const color = this.salon!.themeColor || '#111111';
          (this.el.nativeElement as HTMLElement).style.setProperty('--accent', color);
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

  loadStaff(): void {
    this.bookingService.getStaff(this.subdomain).subscribe({
      next: (res) => {
        if (res.success) this.staffList = res.data;
      },
    });
  }

  selectStaff(staff: BookingStaff): void {
    this.selectedStaff = staff;
    this.selectedServices = [];
    this.selectedDate = '';
    this.selectedSlot = '';
    this.slots = [];
    this.loadServicesForStaff(staff.id);
  }

  loadServicesForStaff(staffId: string): void {
    this.bookingService.getServices(this.subdomain, staffId).subscribe({
      next: (res) => {
        if (res.success) this.services = res.data;
      },
    });
  }

  toggleService(service: BookingService): void {
    const idx = this.selectedServices.findIndex(s => s.id === service.id);
    if (idx >= 0) {
      this.selectedServices = this.selectedServices.filter(s => s.id !== service.id);
    } else {
      this.selectedServices = [...this.selectedServices, service];
    }
    this.selectedSlot = '';
    this.slots = [];
    this.loadSlotsIfReady();
  }

  isServiceSelected(service: BookingService): boolean {
    return this.selectedServices.some(s => s.id === service.id);
  }

  get totalDuration(): number {
    return this.selectedServices.reduce((sum, s) => sum + s.durationMinutes, 0);
  }

  get totalPrice(): number {
    return this.selectedServices.reduce((sum, s) => sum + (s.price ?? 0), 0);
  }

  get totalCurrency(): string {
    return this.selectedServices[0]?.currency ?? 'TRY';
  }

  get hasMixedCurrencies(): boolean {
    if (this.selectedServices.length <= 1) return false;
    const first = this.selectedServices[0]?.currency ?? 'TRY';
    return this.selectedServices.some(s => (s.currency ?? 'TRY') !== first);
  }

  get formattedTotalPrice(): string {
    if (this.selectedServices.length === 0) return '—';
    if (this.hasMixedCurrencies) {
      const byCurrency = new Map<string, number>();
      for (const s of this.selectedServices) {
        const c = s.currency ?? 'TRY';
        byCurrency.set(c, (byCurrency.get(c) ?? 0) + (s.price ?? 0));
      }
      return Array.from(byCurrency.entries())
        .map(([c, total]) => total > 0 ? this.formatPrice(total, c) : null)
        .filter(Boolean)
        .join(' + ') || '—';
    }
    if (this.totalPrice <= 0) return '—';
    return this.formatPrice(this.totalPrice, this.totalCurrency);
  }

  get selectedServiceNames(): string {
    return this.selectedServices.map(s => this.getServiceName(s)).join(' + ') || '—';
  }

  onDateChange(date: string): void {
    this.selectedDate = date;
    this.selectedSlot = '';
    this.slots = [];
    this.loadSlotsIfReady();
  }

  loadSlotsIfReady(): void {
    if (!this.selectedStaff || this.selectedServices.length === 0 || !this.selectedDate)
      return;

    const dateUtc = this.selectedDate + 'T00:00:00Z';
    const serviceIds = this.selectedServices.map(s => s.id);

    this.bookingService
      .getAvailableSlots(this.subdomain, this.selectedStaff.id, serviceIds, dateUtc)
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
      !!this.selectedStaff &&
      this.selectedServices.length > 0 &&
      !!this.selectedDate &&
      !!this.selectedSlot
    );
  }

  onSubmit(): void {
    if (this.customerForm.invalid || !this.canProceed()) return;

    this.isSubmitting = true;
    this.errorMessage = '';

    const { fullName, phone, email, notes } = this.customerForm.value;
    const serviceIds = this.selectedServices.map(s => s.id);

    this.bookingService
      .createAppointment(this.subdomain, {
        fullName,
        phone,
        email,
        staffId: this.selectedStaff!.id,
        serviceId: serviceIds[0],
        serviceIds,
        startTime: this.selectedSlot,
        notes,
      })
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.router.navigate(
              ['/randevu', this.subdomain, res.appointmentId],
              { queryParams: { phone } }
            );
          } else {
            this.errorMessage = res.message;
          }
          this.isSubmitting = false;
        },
        error: () => {
          this.errorMessage = 'Bir hata oluştu. Lütfen tekrar deneyin.';
          this.isSubmitting = false;
        },
      });
  }

  formatTime(dateStr: string): string {
    return new Date(dateStr).toLocaleTimeString('tr-TR', {
      hour: '2-digit', minute: '2-digit', timeZone: 'Europe/Istanbul',
    });
  }

  formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString('tr-TR', {
      weekday: 'long', day: 'numeric', month: 'long', timeZone: 'Europe/Istanbul',
    });
  }

  formatPrice(price: number, currency: string): string {
    const lang = this.langService.lang();
    const unspecified = { tr: 'Belirtilmemiş', en: 'Unspecified', ru: 'Не указано' }[lang] ?? 'Belirtilmemiş';
    if (!price || price <= 0) return unspecified;
    return new Intl.NumberFormat(this.langService.dateLocale, {
      style: 'currency',
      currency: currency || 'TRY',
    }).format(price);
  }

  formatPriceRange(service: BookingService): string {
    if (service.hasMixedCurrencies) return this.langService.t('booking.priceVaries') || 'Personele göre değişir';
    const currency = service.currency || 'TRY';
    if (!service.minPrice && !service.maxPrice) return this.formatPrice(service.price, currency);
    const min = service.minPrice ?? service.price;
    const max = service.maxPrice ?? service.price;
    if (min === max) return this.formatPrice(min, currency);
    return `${this.formatPrice(min, currency)} – ${this.formatPrice(max, currency)}`;
  }

  getServiceName(service: BookingService): string {
    const lang = this.langService.lang();
    if (lang === 'en' && service.nameEn) return service.nameEn;
    if (lang === 'ru' && service.nameRu) return service.nameRu;
    return service.name;
  }

  encodeURI(s: string): string { return encodeURIComponent(s); }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  sendOtp(): void {
    const phone = this.customerForm.get('phone')?.value;
    if (!phone) return;

    this.isSendingOtp = true;
    this.otpError = '';
    this.otpSuccess = '';

    this.bookingService.sendOtp(phone).subscribe({
      next: (res) => {
        if (res.success) {
          this.otpSent = true;
          this.otpSuccess = 'Doğrulama kodu WhatsApp ile gönderildi.';
          this.startTimer();
        }
        this.isSendingOtp = false;
      },
      error: (err) => {
        this.otpError = err.status < 500 && err.error?.message
          ? err.error.message
          : 'Bir hata oluştu. Lütfen tekrar deneyin.';
        this.isSendingOtp = false;
      },
    });
  }

  verifyOtp(): void {
    const phone = this.customerForm.get('phone')?.value;
    if (!phone || !this.otpCode) return;

    this.isVerifyingOtp = true;
    this.otpError = '';

    this.bookingService.verifyOtp(phone, this.otpCode).subscribe({
      next: (res) => {
        if (res.success) {
          this.otpVerified = true;
          this.otpSuccess = 'Telefon doğrulandı! ✓';
        }
        this.isVerifyingOtp = false;
      },
      error: (err) => {
        this.otpError = err.status < 500 && err.error?.message
          ? err.error.message
          : 'Bir hata oluştu. Lütfen tekrar deneyin.';
        this.isVerifyingOtp = false;
      },
    });
  }

  startTimer(): void {
    this.otpTimer = 120;
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
