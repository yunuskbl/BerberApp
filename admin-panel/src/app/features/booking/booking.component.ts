import { Component, OnInit, OnDestroy, ElementRef, Renderer2 } from '@angular/core';
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
import { PackageService } from '../../core/services/package.service';
import { PackageBookingContext } from '../../core/models/package.model';

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

  /**
   * Paketten randevu kipi. Müşteri paket takip bağlantısından (?paket=…) ya da
   * telefonunu doğruladıktan sonra "Paketimden kullan" ile gelir: hizmet
   * paketin hizmeti, ücret alınmaz, ad/telefon/doğrulama istenmez.
   */
  pkg: PackageBookingContext | null = null;
  /** Paket kipine geçilemediyse nedeni (ör. tüm seanslar için randevu var). */
  pkgNotice = '';
  /** Doğrulanmış müşterinin seçili hizmete uyan, randevu alınabilir paketleri. */
  pkgOffers: PackageBookingContext[] = [];

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

  // Türkiye yerel tarihini kullan — toISOString() UTC'ye çevirdiği için
  // gece yarısı 00:00-03:00 arası bir önceki günü "bugün" olarak gösterip
  // geçmiş tarihe randevu seçilmesine izin verebiliyordu.
  get minDate(): string {
    return new Date().toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' });
  }

  get maxDate(): string {
    const date = new Date();
    date.setDate(date.getDate() + 15);
    return date.toLocaleDateString('en-CA', { timeZone: 'Europe/Istanbul' });
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
    private packageService: PackageService,
  ) {
    this.customerForm = this.fb.group({
      fullName: ['', [Validators.required, Validators.maxLength(30)]],
      phone: ['', [Validators.required]],
      email: ['', [Validators.email, Validators.maxLength(80)]],
      notes: ['', [Validators.maxLength(200)]],
      // Aydınlatma metninin okunduğu teyidi — zorunlu.
      kvkkAccepted: [false, [Validators.requiredTrue]],
      // Pazarlama için açık rıza — opsiyonel, varsayılan kapalı.
      marketingConsent: [false],
    });
  }

  ngOnInit(): void {
    this.subdomain = this.route.snapshot.paramMap.get('subdomain') || '';
    this.loadSavedCustomer();
    this.updatePhoneValidators();
    this.loadSalon();
    const token = this.route.snapshot.queryParamMap.get('paket');
    if (token) this.loadPackageContext(token);
  }

  // ─── Paketten randevu ────────────────────────────────────

  private loadPackageContext(token: string): void {
    this.packageService.getBookingContext(token).subscribe({
      next: r => {
        if (!r.success) return;
        if (r.data.canBook) this.enterPackageMode(r.data);
        else this.pkgNotice = r.data.reason || '';
      },
      error: () => { this.pkgNotice = 'Paket bulunamadı.'; },
    });
  }

  enterPackageMode(ctx: PackageBookingContext): void {
    this.pkg = ctx;
    this.pkgOffers = [];
    this.pkgNotice = '';
    // Kimlik paketten geliyor; bu alanlar formun geçerliliğine katılmasın.
    for (const c of ['fullName', 'phone', 'email']) this.customerForm.get(c)?.disable();
    this.errorMessage = '';
    if (this.selectedStaff) {
      this.applyPackageService();
    } else {
      this.loadStaff();
    }
  }

  leavePackageMode(): void {
    this.pkg = null;
    for (const c of ['fullName', 'phone', 'email']) this.customerForm.get(c)?.enable();
    this.router.navigate([], { queryParams: { paket: null }, queryParamsHandling: 'merge', replaceUrl: true });
    this.loadStaff();
    if (this.selectedStaff) this.loadServicesForStaff(this.selectedStaff.id);
  }

  /** Seçimi paketin hizmetine indirger; personel o hizmeti vermiyorsa söyler. */
  private applyPackageService(): void {
    if (!this.pkg) return;
    const before = this.selectedServices.map(s => s.id).join();
    const svc = this.pkg.serviceId
      ? this.services.find(s => s.id === this.pkg!.serviceId)
      : this.selectedServices[0];
    this.selectedServices = svc ? [svc] : [];
    this.pkgNotice = !svc && this.pkg.serviceId && this.selectedStaff
      ? this.langService.t('booking.pkg.staffMismatch')
      : '';
    if (this.selectedServices.map(s => s.id).join() !== before) {
      this.selectedSlot = '';
      this.slots = [];
      this.loadSlotsIfReady();
    }
  }

  /** Paket kipinde hizmet listesi yalnızca paketin hizmeti. */
  get visibleServices(): BookingService[] {
    return this.pkg?.serviceId ? this.services.filter(s => s.id === this.pkg!.serviceId) : this.services;
  }

  /** Telefon doğrulandı: bu salonda, seçili hizmete uyan paketi var mı? */
  private checkPackagesForVerifiedPhone(): void {
    if (this.pkg) return;
    this.packageService.lookupPackages(this.subdomain, this.fullPhone).subscribe({
      next: r => {
        if (!r.success) return;
        const selected = new Set(this.selectedServices.map(s => s.id));
        this.pkgOffers = (r.data ?? []).filter(p =>
          p.canBook && (!p.serviceId || selected.has(p.serviceId)));
      },
      error: () => { /* paket önerisi yalnızca kolaylık */ },
    });
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

  /**
   * Kutunun kaldırılması anında etkili olur.
   *
   * Eskiden temizlik yalnızca randevu başarıyla tamamlandığında yapılıyordu:
   * kutuyu kaldırıp randevuyu tamamlamadan ayrılan kullanıcının bilgileri
   * kayıtlı kalıyor, bir sonraki ziyarette geri yüklenip kutu yeniden
   * işaretli geliyordu — kullanıcıya "seçmediğim halde hatırlıyor" gibi
   * görünen davranış buydu.
   */
  onRememberMeChange(): void {
    if (this.rememberMe) return;
    try {
      localStorage.removeItem(this.STORAGE_KEY);
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
    // OTP geri sayımı yalnızca sıfıra ulaşınca temizleniyordu; kullanıcı
    // sayaç dolmadan sayfadan ayrılırsa interval yıkılmış bileşen üzerinde
    // çalışmaya devam ediyordu.
    if (this.timerInterval) {
      clearInterval(this.timerInterval);
      this.timerInterval = undefined;
    }
  }

  onPhoneInput(event: Event): void {
    if (this.fullPhone !== this.verifiedPhone) {
      this.otpVerified = false;
    }
    this.otpSent = false;
    this.otpCode = '';
    this.otpError = '';
    this.otpSuccess = '';
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
    // Paket kipinde yalnızca paketin hizmetini veren personel listelenir.
    this.bookingService.getStaff(this.subdomain, this.pkg?.serviceId).subscribe({
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
        if (this.pkg) this.applyPackageService();
      },
    });
  }

  toggleService(service: BookingService): void {
    if (this.pkg) return;   // paket kipinde hizmet paketten, değiştirilemez
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
    if (this.pkg) return this.langService.t('booking.pkg.free');
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
    if (this.pkg) { this.submitPackageBooking(); return; }

    this.isSubmitting = true;
    this.errorMessage = '';

    const { fullName, email, notes, marketingConsent } = this.customerForm.value;
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
        marketingConsent: !!marketingConsent,
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

          // Sunucu tarafındaki durum değişmiş olabilir — özellikle çakışmada
          // (409) ekrandaki slot listesi bayat demektir: saat, sayfa açıldıktan
          // sonra dolmuştur. Listeyi tazeleyip seçimi düşürüyoruz ki kullanıcı
          // aynı dolu saati tekrar tekrar denemesin.
          this.loadSlotsIfReady();
          if (err.status === 409) this.selectedSlot = '';

          setTimeout(() => {
            document.querySelector('.error-message')?.scrollIntoView({ behavior: 'smooth', block: 'center' });
          }, 50);
        },
      });
  }

  /** Paketten randevu: kimlik paket anahtarından, ad/telefon/kod gönderilmez. */
  private submitPackageBooking(): void {
    const pkg = this.pkg!;
    this.isSubmitting = true;
    this.errorMessage = '';
    const serviceId = this.selectedServices[0].id;
    this.bookingService
      .createAppointment(this.subdomain, {
        fullName: '',
        phone: '',
        staffId: this.selectedStaff!.id,
        serviceId,
        serviceIds: [serviceId],
        startTime: this.selectedSlot,
        notes: this.customerForm.get('notes')?.value || undefined,
        packageToken: pkg.token,
      })
      .subscribe({
        next: (res) => {
          this.isSubmitting = false;
          if (res.success) {
            // Randevu paketin "planlanan seansları"nda görünür.
            this.router.navigate(['/paket', pkg.token], { queryParams: { booked: 1 } });
          } else {
            this.errorMessage = res.message;
          }
        },
        error: (err) => {
          this.isSubmitting = false;
          this.errorMessage = err.status < 500 && err.error?.message
            ? err.error.message
            : 'Bir hata oluştu. Lütfen tekrar deneyin.';
          this.loadSlotsIfReady();
          if (err.status === 409) this.selectedSlot = '';
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

    const email = this.customerForm.get('email')?.value?.trim() || '';
    this.bookingService.sendOtp(phone, email || undefined, this.subdomain || undefined).subscribe({
      next: (res) => {
        if (res.success) {
          this.otpSent = true;
          // Sunucu mesajı Türkçe geldiği için yerel çeviri tercih ediliyor;
          // başka dildeki müşteri kendi dilinde okuyor.
          this.otpSuccess = this.langService.t(email ? 'otp.sentEmail' : 'otp.sentSms');
          this.startTimer();
        }
        this.isSendingOtp = false;
      },
      error: (err) => {
        this.otpError = err.error?.message || this.langService.t('otp.error');
        this.isSendingOtp = false;
      },
    });
  }

  /**
   * Doğrulanmış numaraya ait kayıtlı adı forma yazar.
   * Yalnızca OTP sonrası çağrılır: numaranın sahibi olduğu kanıtlanmadan
   * kimsenin adı gösterilmemeli. Kullanıcı kendi yazdıysa üzerine yazmaz.
   */
  private prefillVerifiedCustomer(): void {
    this.bookingService.lookupCustomer(this.subdomain, this.fullPhone).subscribe({
      next: (res: any) => {
        const current = (this.customerForm.get('fullName')?.value || '').trim();
        if (res?.success && res.data?.fullName && !current) {
          this.customerForm.patchValue({ fullName: res.data.fullName });
        }
      },
      error: () => { /* ad getirilemezse kullanıcı kendisi yazar */ },
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
          this.verifiedPhone = this.fullPhone;
          this.otpSuccess = this.langService.t('otp.phoneVerified');
          // Numaranın sahibi olduğu doğrulandı; kayıtlı adı şimdi getirebiliriz.
          this.prefillVerifiedCustomer();
          // ve bu hizmet için aktif paketi varsa önerebiliriz.
          this.checkPackagesForVerifiedPhone();
        }
        this.isVerifyingOtp = false;
      },
      error: (err) => {
        this.otpError = err.status < 500 && err.error?.message
          ? err.error.message
          : this.langService.t('otp.error');
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
