import { Component, OnInit } from '@angular/core';
import { CommonModule, DatePipe } from '@angular/common';
import {
  ReactiveFormsModule,
  FormsModule,
  FormBuilder,
  FormGroup,
  Validators,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { Router, RouterModule } from '@angular/router';
import { LanguageService } from '../../core/services/language.service';
import { TranslatePipe } from '../../shared/pipes/translate.pipe';
import { SalonClosureService, SalonClosure } from '../../core/services/salon-closure.service';
import { CustomCalendarComponent } from '../../shared/components/custom-calendar/custom-calendar.component';
import { catchError, of } from 'rxjs';
import { BranchService } from '../../core/services/branch.service';
import QRCode from 'qrcode';

/* Turkish/generic phone validator */
function phoneValidator(ctrl: AbstractControl): ValidationErrors | null {
  const val: string = (ctrl.value || '').replace(/\s/g, '');
  if (!val) return null; // optional field — no error if empty
  const ok = /^(\+?[0-9]{7,15})$/.test(val);
  return ok ? null : { invalidPhone: true };
}

/* Password strength validators */
function uppercaseValidator(ctrl: AbstractControl): ValidationErrors | null {
  return /[A-Z]/.test(ctrl.value || '') ? null : { noUppercase: true };
}
function numberValidator(ctrl: AbstractControl): ValidationErrors | null {
  return /[0-9]/.test(ctrl.value || '') ? null : { noNumber: true };
}

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, DatePipe, ReactiveFormsModule, FormsModule, RouterModule, TranslatePipe, CustomCalendarComponent],
  templateUrl: './settings.component.html',
  styleUrl: './settings.component.scss',
})
export class SettingsComponent implements OnInit {
  isLoading = true;
  isSaving = false;
  isChangingPass = false;
  isUploadingLogo = false;
  successMessage = '';
  errorMessage = '';
  passSuccess = '';
  passError = '';
  logoUploadError = '';
  logoPreview: string | null = null;
  logoVersion = Date.now();

  salonPhotos: { id: string; url: string }[] = [];
  isUploadingPhoto = false;
  photoUploadError = '';

  // Plus Code
  plusCodeValue = '';
  plusCodeError = '';
  isResolvingCode = false;

  // Salon Kapalı Günler
  closures: SalonClosure[] = [];
  isSavingClosure = false;
  closureError = '';
  closureForm!: FormGroup;

  // QR Kod
  qrCodeDataUrl = '';

  // Bildirim kanalı: 0 = WhatsApp, 1 = Sms
  notificationChannel: 0 | 1 = 0;
  isSavingChannel = false;
  channelSuccess = '';
  channelError = '';

  presetColors = [
    { value: '#111111', label: 'Siyah'   },
    { value: '#1a1a2e', label: 'Lacivert' },
    { value: '#7c3aed', label: 'Mor'     },
    { value: '#2563eb', label: 'Mavi'    },
    { value: '#059669', label: 'Yeşil'   },
    { value: '#dc2626', label: 'Kırmızı' },
    { value: '#d97706', label: 'Turuncu' },
    { value: '#be185d', label: 'Pembe'   },
    { value: '#7c2d12', label: 'Kahve'   },
    { value: '#374151', label: 'Gri'     },
  ];

  salonForm: FormGroup;
  passwordForm: FormGroup;

  get todayDate(): string {
    return new Date().toISOString().split('T')[0];
  }

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private authService: AuthService,
    private router: Router,
    private salonClosureService: SalonClosureService,
    private branchService: BranchService,
    public langService: LanguageService,
  ) {
    this.salonForm = this.fb.group({
      name:              ['', Validators.required],
      phone:             ['', phoneValidator],
      notificationPhone: ['', phoneValidator],
      address:           [''],
      logoUrl:           [''],
      themeColor:        ['#7c3aed'],
      businessType:      [null],
      latitude:          [null as number | null],
      longitude:         [null as number | null],
    });

    this.closureForm = this.fb.group({
      startDate: ['', Validators.required],
      endDate:   [''],
      reason:    [''],
    });

    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: ['', [
          Validators.required,
          Validators.minLength(8),
          uppercaseValidator,
          numberValidator,
        ]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  ngOnInit(): void {
    this.loadSalonInfo();
    this.loadPhotos();
    this.loadClosures();
  }

  /* ─── Photos ─── */
  loadPhotos(): void {
    this.http.get<any>(`${environment.apiUrl}/tenants/photos`).subscribe({
      next: (res) => { if (res.success) this.salonPhotos = res.data.photos; },
    });
  }

  onPhotoSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    this.photoUploadError = '';
    this.isUploadingPhoto = true;
    const formData = new FormData();
    formData.append('file', file);
    this.http.post<any>(`${environment.apiUrl}/tenants/photos`, formData).subscribe({
      next: (res) => {
        if (res.success) this.salonPhotos = [...this.salonPhotos, res.data.photo];
        this.isUploadingPhoto = false;
      },
      error: (err) => {
        this.photoUploadError = err.error?.message || 'Fotoğraf yüklenemedi.';
        this.isUploadingPhoto = false;
      },
    });
    input.value = '';
  }

  deletePhoto(id: string): void {
    this.http.delete<any>(`${environment.apiUrl}/tenants/photos/${id}`).subscribe({
      next: () => { this.salonPhotos = this.salonPhotos.filter(p => p.id !== id); },
    });
  }

  /* ─── Salon Kapalı Günler ─── */
  loadClosures(): void {
    this.salonClosureService.getAll().pipe(
      catchError(() => of({ success: false, data: [] }))
    ).subscribe({
      next: (res) => {
        this.closures = res.success && Array.isArray(res.data) ? res.data : [];
      },
    });
  }

  addClosure(): void {
    if (!this.closureForm.valid) return;
    this.isSavingClosure = true;
    this.closureError = '';
    const { startDate, endDate, reason } = this.closureForm.value;
    this.salonClosureService.create({
      startDate,
      endDate: endDate || startDate,
      reason: reason || undefined
    }).pipe(
      catchError((err) => {
        this.closureError = err?.error?.message || 'Kapalı gün eklenemedi.';
        this.isSavingClosure = false;
        return of(null);
      })
    ).subscribe({
      next: (res) => {
        if (!res) return;
        this.isSavingClosure = false;
        this.closureForm.patchValue({ startDate: '', endDate: '', reason: '' });
        this.loadClosures();
      },
    });
  }

  formatClosureDate(c: SalonClosure): string {
    if (!c.startDate) return '';
    const opts: Intl.DateTimeFormatOptions = { day: 'numeric', month: 'long' };
    const start = new Date(c.startDate + 'T12:00:00');
    if (!c.endDate || c.startDate === c.endDate) {
      return start.toLocaleDateString('tr-TR', { ...opts, year: 'numeric' });
    }
    const end = new Date(c.endDate + 'T12:00:00');
    return `${start.toLocaleDateString('tr-TR', opts)} – ${end.toLocaleDateString('tr-TR', { ...opts, year: 'numeric' })}`;
  }

  removeClosure(id: string): void {
    this.salonClosureService.delete(id).pipe(
      catchError(() => of(null))
    ).subscribe({
      next: () => this.loadClosures(),
    });
  }

  /* ─── QR Kod ─── */
  async generateQrCode(): Promise<void> {
    if (!this.bookingUrl) return;
    try {
      this.qrCodeDataUrl = await QRCode.toDataURL(this.bookingUrl, {
        width: 200,
        margin: 2,
        color: { dark: '#111827', light: '#ffffff' },
      });
    } catch { /* silently ignore */ }
  }

  downloadQrCode(): void {
    if (!this.qrCodeDataUrl) return;
    const link = document.createElement('a');
    link.download = 'randevu-qr.png';
    link.href = this.qrCodeDataUrl;
    link.click();
  }

  /* ─── Salon ─── */
  loadSalonInfo(): void {
    this.http.get<any>(`${environment.apiUrl}/tenants/me`).subscribe({
      next: (res) => {
        if (res.success) {
          this.salonForm.patchValue({
            name:              res.data.name,
            phone:             res.data.phone,
            notificationPhone: res.data.notificationPhone,
            address:           res.data.address,
            logoUrl:           res.data.logoUrl ?? '',
            themeColor:        res.data.themeColor ?? '#7c3aed',
            businessType:      res.data.businessType ?? null,
            latitude:          res.data.latitude ?? null,
            longitude:         res.data.longitude ?? null,
          });
          if (res.data.logoUrl) this.logoPreview = res.data.logoUrl;
          this.notificationChannel = res.data.preferredNotificationChannel ?? 0;
          setTimeout(() => this.generateQrCode(), 0);
          // DB'den gelen planı localStorage ile senkronize et — şube bağlamındayken yapma
          if (res.data.planType && !this.branchService.activeBranch()) {
            localStorage.setItem('userPlan', res.data.planType);
          }
        }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
  }

  // Türkiye il ve büyük ilçe koordinatları (offline lookup)
  private readonly TR_CITIES: Record<string, [number, number]> = {
    'adana': [37.0000, 35.3213], 'adıyaman': [37.7648, 38.2786],
    'afyon': [38.7507, 30.5567], 'afyonkarahisar': [38.7507, 30.5567],
    'ağrı': [39.7191, 43.0503], 'amasya': [40.6499, 35.8353],
    'ankara': [39.9334, 32.8597], 'antalya': [36.8969, 30.7133],
    'artvin': [41.1828, 41.8183], 'aydın': [37.8456, 27.8454],
    'balıkesir': [39.6484, 27.8826], 'bilecik': [40.1506, 29.9790],
    'bingöl': [38.8858, 40.4983], 'bitlis': [38.3938, 42.1231],
    'bolu': [40.7360, 31.6060], 'burdur': [37.7205, 30.2911],
    'bursa': [40.1829, 29.0669],
    'çanakkale': [40.1553, 26.4142], 'çankırı': [40.6013, 33.6134],
    'çorum': [40.5506, 34.9556], 'denizli': [37.7765, 29.0864],
    'diyarbakır': [37.9144, 40.2306], 'edirne': [41.6771, 26.5557],
    'elazığ': [38.6810, 39.2264], 'erzincan': [39.7500, 39.4917],
    'erzurum': [39.9035, 41.2677], 'eskişehir': [39.7767, 30.5206],
    'gaziantep': [37.0662, 37.3833], 'giresun': [40.9128, 38.3895],
    'gümüşhane': [40.4386, 39.4814], 'hakkari': [37.5744, 43.7408],
    'hatay': [36.4018, 36.3498], 'antakya': [36.2025, 36.1603],
    'ısparta': [37.7648, 30.5566], 'isparta': [37.7648, 30.5566],
    'istanbul': [41.0082, 28.9784], 'izmir': [38.4237, 27.1428],
    'kars': [40.6013, 43.0975], 'kastamonu': [41.3887, 33.7827],
    'kayseri': [38.7312, 35.4787], 'kırklareli': [41.7333, 27.2167],
    'kırşehir': [39.1425, 34.1709], 'kocaeli': [40.7654, 29.9408],
    'izmit': [40.7654, 29.9408], 'konya': [37.8714, 32.4846],
    'kütahya': [39.4167, 29.9833], 'malatya': [38.3552, 38.3095],
    'manisa': [38.6191, 27.4289], 'maraş': [37.5858, 36.9371],
    'kahramanmaraş': [37.5858, 36.9371], 'mardin': [37.3212, 40.7245],
    'muğla': [37.2153, 28.3636], 'muş': [38.9462, 41.7539],
    'nevşehir': [38.6939, 34.6857], 'niğde': [37.9667, 34.6833],
    'ordu': [40.9860, 37.8797], 'rize': [41.0201, 40.5234],
    'sakarya': [40.6940, 30.4358], 'adapazarı': [40.7731, 30.3948],
    'samsun': [41.2928, 36.3313], 'siirt': [37.9333, 41.9500],
    'sinop': [42.0231, 35.1531], 'sivas': [39.7477, 37.0179],
    'tekirdağ': [40.9781, 27.5115], 'tokat': [40.3167, 36.5500],
    'trabzon': [41.0015, 39.7178], 'tunceli': [39.1079, 39.5482],
    'şanlıurfa': [37.1591, 38.7969], 'urfa': [37.1591, 38.7969],
    'uşak': [38.6823, 29.4082], 'van': [38.4891, 43.4089],
    'yozgat': [39.8181, 34.8147], 'zonguldak': [41.4564, 31.7987],
    'aksaray': [38.3687, 34.0370], 'bayburt': [40.2552, 40.2249],
    'karaman': [37.1759, 33.2287], 'kırıkkale': [39.8468, 33.5153],
    'batman': [37.8812, 41.1351], 'şırnak': [37.5164, 42.4611],
    'bartın': [41.6344, 32.3375], 'ardahan': [41.1105, 42.7022],
    'iğdır': [39.9167, 44.0333], 'yalova': [40.6500, 29.2667],
    'karabük': [41.2061, 32.6204], 'kilis': [36.7184, 37.1212],
    'osmaniye': [37.2167, 36.1833], 'düzce': [40.8438, 31.1565],
    // Büyük ilçeler
    'nilüfer': [40.2167, 28.9833], 'osmangazi': [40.1963, 29.0608],
    'yıldırım': [40.1833, 29.1000], 'gemlik': [40.4333, 29.1500],
    'mudanya': [40.3750, 28.8833], 'kemalpaşa': [38.4285, 27.4225],
    'bornova': [38.4667, 27.2167], 'buca': [38.3833, 27.1833],
    'karşıyaka': [38.4600, 27.1100], 'konak': [38.4127, 27.1384],
    'gaziemir': [38.3244, 27.1358], 'torbalı': [38.1597, 27.3622],
    'alanya': [36.5434, 32.0022], 'manavgat': [36.7858, 31.4414],
    'serik': [36.9170, 31.1033], 'konyaaltı': [36.8667, 30.6333],
    'muratpaşa': [36.8782, 30.7000], 'kepez': [37.0167, 30.7167],
    'kadıköy': [40.9811, 29.0310], 'beşiktaş': [41.0422, 29.0078],
    'şişli': [41.0602, 28.9878], 'üsküdar': [41.0228, 29.0151],
    'bakırköy': [40.9781, 28.8736], 'maltepe': [40.9333, 29.1333],
    'pendik': [40.8762, 29.2333], 'gebze': [40.8017, 29.4303],
    'darıca': [40.7667, 29.3667], 'gölcük': [40.7167, 29.8167],
    'çayırova': [40.8000, 29.3833], 'selçuklu': [37.9167, 32.4667],
    'meram': [37.8333, 32.4333], 'karatay': [37.8833, 32.5167],
    'melikgazi': [38.7000, 35.5167], 'kocasinan': [38.7167, 35.4667],
    'odunpazarı': [39.7833, 30.5167], 'tepebaşı': [39.7667, 30.5000],
    'pamukkale': [37.9208, 29.1183], 'merkezefendi': [37.7833, 29.0833],
    'atakum': [41.3333, 36.3000], 'ilkadım': [41.2833, 36.3333],
    'canik': [41.2333, 36.4000], 'mezitli': [36.7833, 34.5833],
    'toroslar': [36.8500, 34.6167], 'yenişehir': [36.8167, 34.6500],
  };

  private lookupCity(reference: string): [number, number] | null {
    // "Nilüfer, Bursa" → ['nilüfer', 'bursa'] — try each part
    const parts = reference.split(/[,\/]/).map(p => p.trim().toLowerCase());
    for (const part of parts) {
      const coords = this.TR_CITIES[part];
      if (coords) return coords;
    }
    return null;
  }

  /* ─── Plus Code ─── */
  applyPlusCode(): void {
    // eslint-disable-next-line @typescript-eslint/no-require-imports
    const { OpenLocationCode } = require('open-location-code');
    const olc = new OpenLocationCode();

    this.plusCodeError = '';
    const raw = this.plusCodeValue.trim();
    if (!raw) return;

    // "MPHP+RG Alanya" → code + optional reference
    const spaceIdx = raw.indexOf(' ');
    const code      = (spaceIdx > -1 ? raw.slice(0, spaceIdx) : raw).toUpperCase();
    const reference = spaceIdx > -1 ? raw.slice(spaceIdx + 1).trim() : '';

    if (!olc.isValid(code)) {
      this.plusCodeError = 'Geçersiz Plus Code formatı.';
      return;
    }

    if (olc.isFull(code)) {
      const area = olc.decode(code);
      this.salonForm.patchValue({ latitude: area.latitudeCenter, longitude: area.longitudeCenter });
      this.plusCodeValue = '';
      return;
    }

    // Kısa kod → yerel tablodan referans koordinatı bul
    if (!reference) {
      this.plusCodeError = 'Kısa kodlar için şehir adı gerekli. Örnek: "MPHP+RG Alanya"';
      return;
    }

    const refCoords = this.lookupCity(reference);
    if (!refCoords) {
      this.plusCodeError = `"${reference}" bulunamadı. Tam Plus Code kullanın (örn: 8FVC9G8F+6W).`;
      return;
    }

    const fullCode = olc.recoverNearest(code, refCoords[0], refCoords[1]);
    const area = olc.decode(fullCode);
    this.salonForm.patchValue({ latitude: area.latitudeCenter, longitude: area.longitudeCenter });
    this.plusCodeValue = '';
  }

  /* ─── Bildirim Kanalı ─── */
  onSaveNotificationChannel(): void {
    this.isSavingChannel = true;
    this.channelSuccess = '';
    this.channelError = '';
    this.http.put<any>(`${environment.apiUrl}/settings/notification-channel`, { channel: this.notificationChannel }).subscribe({
      next: (res) => {
        if (res.success) {
          this.channelSuccess = this.langService.t('settings.notif.saved');
          setTimeout(() => (this.channelSuccess = ''), 3000);
        }
        this.isSavingChannel = false;
      },
      error: (err) => {
        this.channelError = err.error?.message || 'Hata oluştu.';
        this.isSavingChannel = false;
      },
    });
  }

  onSaveSalon(): void {
    if (this.salonForm.invalid) return;
    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';
    this.http.put<any>(`${environment.apiUrl}/tenants`, this.salonForm.value).subscribe({
      next: (res) => {
        if (res.success) {
          this.successMessage = this.langService.t('settings.salon.saved');
          setTimeout(() => (this.successMessage = ''), 3000);
        }
        this.isSaving = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Hata oluştu.';
        this.isSaving = false;
      },
    });
  }

  /* ─── Password ─── */
  onChangePassword(): void {
    if (this.passwordForm.invalid) return;
    this.isChangingPass = true;
    this.passSuccess = '';
    this.passError = '';
    const { currentPassword, newPassword } = this.passwordForm.value;
    this.http.post<any>(`${environment.apiUrl}/auth/change-password`, { currentPassword, newPassword }).subscribe({
      next: (res) => {
        if (res.success) {
          this.passSuccess = this.langService.t('settings.password.changed');
          this.passwordForm.reset();
          setTimeout(() => (this.passSuccess = ''), 3000);
        }
        this.isChangingPass = false;
      },
      error: (err) => {
        this.passError = err.error?.message || 'Şifre değiştirilemedi.';
        this.isChangingPass = false;
      },
    });
  }

  passwordMatchValidator(form: FormGroup) {
    const np = form.get('newPassword')?.value;
    const cp = form.get('confirmPassword')?.value;
    return np === cp ? null : { passwordMismatch: true };
  }

  /* ─── Password rule getters ─── */
  get newPassValue(): string { return this.passwordForm.get('newPassword')?.value || ''; }
  get passRuleMinLen(): boolean  { return this.newPassValue.length >= 8; }
  get passRuleUpper(): boolean   { return /[A-Z]/.test(this.newPassValue); }
  get passRuleNumber(): boolean  { return /[0-9]/.test(this.newPassValue); }
  get newPassTouched(): boolean  { return !!this.passwordForm.get('newPassword')?.touched; }

  /* ─── Logo ─── */
  onLogoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;
    const file = input.files[0];
    this.logoUploadError = '';
    this.isUploadingLogo = true;
    const reader = new FileReader();
    reader.onload = (e) => { this.logoPreview = e.target?.result as string; };
    reader.readAsDataURL(file);
    const formData = new FormData();
    formData.append('file', file);
    this.http.post<any>(`${environment.apiUrl}/tenants/logo`, formData).subscribe({
      next: (res) => {
        if (res.success) {
          this.salonForm.patchValue({ logoUrl: res.data.logoUrl });
          this.logoPreview = res.data.logoUrl;
          this.logoVersion = Date.now();
        }
        this.isUploadingLogo = false;
      },
      error: (err) => {
        this.logoUploadError = err.error?.message || 'Logo yüklenemedi.';
        this.isUploadingLogo = false;
      },
    });
    input.value = '';
  }

  /* ─── Plan ─── */
  get userPlan(): string { return this.authService.getUserPlan(); }

  get planDisplayInfo(): { name: string; icon: string; color: string } {
    const map: Record<string, { name: string; icon: string; color: string }> = {
      Baslangic:   { name: 'Başlangıç',   icon: '🌱', color: '#6b7280' },
      Profesyonel: { name: 'Profesyonel', icon: '⚡', color: '#7c3aed' },
      Premium:     { name: 'Premium',     icon: '👑', color: '#d97706' },
    };
    return map[this.userPlan] ?? { name: this.userPlan, icon: '📦', color: '#6b7280' };
  }

  /* ─── Links ─── */
  copied = false;
  copiedMain = false;

  get user() { return this.authService.getUser(); }
  get bookingUrl(): string {
    const sub = this.authService.getUser()?.subdomain;
    return sub ? `${window.location.origin}/book/${sub}` : '';
  }

  copyLink(): void {
    navigator.clipboard.writeText(this.bookingUrl);
    this.copied = true;
    setTimeout(() => (this.copied = false), 2000);
  }

  copyMainLink(): void {
    navigator.clipboard.writeText(this.bookingUrl);
    this.copiedMain = true;
    setTimeout(() => (this.copiedMain = false), 2000);
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
