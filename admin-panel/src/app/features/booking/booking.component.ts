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
  AbstractControl,
  ValidatorFn,
  ValidationErrors,
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
  scrolledToBottom = false;
  isSubmitting = false;
  isSuccess = false;
  errorMessage = '';
  bookingResult: any = null;

  lightboxPhoto: string | null = null;
  lightboxIndex = 0;

  onLeftScroll(e: Event): void {
    const el = e.target as HTMLElement;
    this.scrolledToBottom = el.scrollTop + el.clientHeight >= el.scrollHeight - 8;
  }

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

  rememberMe = false;

  countries = [
    { flag: '🇹🇷', dial: '+90',  name: 'Türkiye',     placeholder: '5XX XXX XX XX',  minDigits: 10, maxDigits: 11 },
    { flag: '🇩🇪', dial: '+49',  name: 'Almanya',      placeholder: '1XX XXXXXXXX',   minDigits: 10, maxDigits: 12 },
    { flag: '🇳🇱', dial: '+31',  name: 'Hollanda',     placeholder: '6XX XXX XXXX',   minDigits:  9, maxDigits: 10 },
    { flag: '🇧🇪', dial: '+32',  name: 'Belçika',      placeholder: '4XX XX XX XX',   minDigits:  8, maxDigits:  9 },
    { flag: '🇦🇹', dial: '+43',  name: 'Avusturya',    placeholder: '6XX XXXXXXX',    minDigits:  7, maxDigits: 13 },
    { flag: '🇨🇭', dial: '+41',  name: 'İsviçre',      placeholder: '7X XXX XX XX',   minDigits:  9, maxDigits:  9 },
    { flag: '🇫🇷', dial: '+33',  name: 'Fransa',       placeholder: '6X XX XX XX XX', minDigits:  9, maxDigits: 10 },
    { flag: '🇬🇧', dial: '+44',  name: 'İngiltere',    placeholder: '7XXX XXXXXX',    minDigits: 10, maxDigits: 11 },
    { flag: '🇸🇦', dial: '+966', name: 'S. Arabistan', placeholder: '5XX XXX XXXX',   minDigits:  9, maxDigits:  9 },
    { flag: '🇦🇿', dial: '+994', name: 'Azerbaycan',   placeholder: '5X XXX XXXX',    minDigits:  9, maxDigits:  9 },
    { flag: '🇷🇺', dial: '+7',   name: 'Rusya',        placeholder: '9XX XXX XXXX',   minDigits: 10, maxDigits: 10 },
    { flag: '🇺🇦', dial: '+380', name: 'Ukrayna',      placeholder: '9X XXX XXXX',    minDigits:  9, maxDigits:  9 },
    { flag: '🇺🇸', dial: '+1',   name: 'ABD',          placeholder: 'XXX XXX XXXX',   minDigits: 10, maxDigits: 10 },
  ];

  selectedCountry = this.countries[0];
  isCountryDropdownOpen = false;

  get fullPhone(): string {
    const local = (this.customerForm.get('phone')?.value ?? '').replace(/[\s\-\(\)]/g, '');
    if (!local) return '';
    const dial = this.selectedCountry.dial;
    if (dial === '+90' && local.startsWith('0')) return dial + local.slice(1);
    return dial + local;
  }

  selectCountry(c: typeof this.countries[0]): void {
    this.selectedCountry = c;
    this.isCountryDropdownOpen = false;
    this.updatePhoneValidators();
    this.otpSent = false;
    this.otpVerified = false;
    this.verifiedPhone = '';
    this.otpCode = '';
    this.otpError = '';
    this.otpSuccess = '';
    this.customerFound = false;
  }

  private buildPhoneValidator(country: typeof this.countries[0]): ValidatorFn {
    return (ctrl: AbstractControl): ValidationErrors | null => {
      const raw = (ctrl.value ?? '') as string;
      const digits = raw.replace(/[\s\-\(\)]/g, '');
      if (!digits) return null;
      if (!/^[0-9]+$/.test(digits)) return { phoneFormat: true };
      if (digits.length < country.minDigits || digits.length > country.maxDigits)
        return { phoneLength: { min: country.minDigits, max: country.maxDigits } };
      return null;
    };
  }

  private updatePhoneValidators(): void {
    const ctrl = this.customerForm.get('phone');
    if (!ctrl) return;
    ctrl.setValidators([Validators.required, this.buildPhoneValidator(this.selectedCountry)]);
    ctrl.updateValueAndValidity();
  }

  get phoneLengthError(): string {
    const { name, minDigits, maxDigits } = this.selectedCountry;
    return minDigits === maxDigits
      ? `${name} numarası ${minDigits} haneli olmalıdır`
      : `${name} numarası ${minDigits}–${maxDigits} haneli olmalıdır`;
  }

  customerFound = false;
  isLookingUp = false;
  private phoneSubject$ = new Subject<string>();
  private destroy$ = new Subject<void>();

  private readonly STORAGE_KEY = 'ayarliyo_customer';

  otpSent = false;
  otpVerified = false;
  private verifiedPhone = '';
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
      phone: ['', [Validators.required]],
      email: ['', [Validators.email, Validators.maxLength(80)]],
      notes: ['', [Validators.maxLength(200)]],
      kvkkAccepted: [false, [Validators.requiredTrue]],
    });
  }

  ngOnInit(): void {
    this.subdomain = this.route.snapshot.paramMap.get('subdomain') || '';
    this.loadSavedCustomer();
    this.updatePhoneValidators();
    this.loadSalon();
    this.initPhoneLookup();
    const prefilled = this.fullPhone;
    if (prefilled) {
      this.phoneSubject$.next(prefilled);
    }
  }

  private loadSavedCustomer(): void {
    try {
      const raw = localStorage.getItem(this.STORAGE_KEY);
      if (!raw) return;
      const saved = JSON.parse(raw) as { fullName?: string; phone?: string; email?: string; dialCode?: string };
      this.rememberMe = true;
      if (saved.dialCode) {
        const found = this.countries.find(c => c.dial === saved.dialCode);
        if (found) this.selectedCountry = found;
      }
      this.customerForm.patchValue({
        fullName: saved.fullName ?? '',
        phone:    saved.phone    ?? '',
        email:    saved.email    ?? '',
      });
    } catch { /* localStorage erişilemezse sessizce geç */ }
  }

  private saveCustomerIfRemembered(): void {
    try {
      if (this.rememberMe) {
        const { fullName, phone, email } = this.customerForm.value;
        localStorage.setItem(this.STORAGE_KEY, JSON.stringify({
          fullName, phone, email,
          dialCode: this.selectedCountry.dial,
        }));
      } else {
        localStorage.removeItem(this.STORAGE_KEY);
      }
    } catch { /* ignore */ }
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
          this.otpVerified = true;
          this.verifiedPhone = this.fullPhone;
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
    this.customerFound = false;
    if (this.fullPhone !== this.verifiedPhone) {
      this.otpVerified = false;
    }
    this.otpSent = false;
    this.otpCode = '';
    this.otpError = '';
    this.otpSuccess = '';
    this.phoneSubject$.next(this.fullPhone);
  }

  loadSalon(): void {
    this.isLoading = true;
    this.bookingService.getSalon(this.subdomain).subscribe({
      next: (res) => {
        if (res.success) {
          this.salon = res.data;
          this.titleService.setTitle(this.salon!.name + ' - ayarlıyo');
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
            this.slots = res.data;
        },
      });
  }

  selectSlot(slot: BookingSlot): void {
    if (!slot.isAvailable) return;
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

    const { fullName, email, notes } = this.customerForm.value;
    const phone = this.fullPhone;
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
            this.saveCustomerIfRemembered();
            this.router.navigate(
              ['/randevu', this.subdomain, res.appointmentId],
              { queryParams: { phone } }
            );
          } else {
            this.errorMessage = res.message;
          }
          this.isSubmitting = false;
        },
        error: (err) => {
          this.errorMessage = err.status < 500 && err.error?.message
            ? err.error.message
            : 'Bir hata oluştu. Lütfen tekrar deneyin.';
          this.isSubmitting = false;
          setTimeout(() => {
            document.querySelector('.error-message')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
          }, 50);
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
    const unspecified = { tr: 'Belirtilmemiş', en: 'Unspecified', ru: 'Не указано', de: 'Nicht angegeben' }[lang] ?? 'Belirtilmemiş';
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

  goBack(): void { this.router.navigate(['/salons']); }

  getInitials(name: string): string {
    return name
      .split(' ')
      .map((n) => n[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  }

  sendOtp(): void {
    const phone = this.fullPhone;
    if (!phone) return;

    this.isSendingOtp = true;
    this.otpError = '';
    this.otpSuccess = '';

    this.bookingService.sendOtp(phone).subscribe({
      next: (res) => {
        if (res.success) {
          this.otpSent = true;
          this.otpSuccess = 'Doğrulama kodu gönderildi.';
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
    const phone = this.fullPhone;
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
