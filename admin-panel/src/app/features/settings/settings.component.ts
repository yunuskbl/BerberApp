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
          // DB'den gelen planı localStorage ile senkronize et (stale JWT sorunu çözümü)
          if (res.data.planType) {
            localStorage.setItem('userPlan', res.data.planType);
          }
        }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
    });
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
      // Tam kod → direkt decode
      const area = olc.decode(code);
      this.salonForm.patchValue({ latitude: area.latitudeCenter, longitude: area.longitudeCenter });
      this.plusCodeValue = '';
      return;
    }

    // Kısa kod → referans şehir gerekli
    if (!reference) {
      this.plusCodeError = 'Kısa kodlar için şehir adı gerekli. Örnek: "MPHP+RG Alanya"';
      return;
    }

    this.isResolvingCode = true;
    const url = `https://nominatim.openstreetmap.org/search?q=${encodeURIComponent(reference)}&format=json&limit=1&accept-language=tr`;
    this.http.get<any[]>(url).subscribe({
      next: (results) => {
        this.isResolvingCode = false;
        if (!results?.length) {
          this.plusCodeError = 'Şehir/bölge bulunamadı. Tam Plus Code kullanın.';
          return;
        }
        const refLat = parseFloat(results[0].lat);
        const refLon = parseFloat(results[0].lon);
        const fullCode = olc.recoverNearest(code, refLat, refLon);
        const area = olc.decode(fullCode);
        this.salonForm.patchValue({ latitude: area.latitudeCenter, longitude: area.longitudeCenter });
        this.plusCodeValue = '';
      },
      error: () => {
        this.isResolvingCode = false;
        this.plusCodeError = 'Şehir çözümlenemedi. Tam Plus Code kullanın.';
      },
    });
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
