import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
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
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslatePipe],
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

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private authService: AuthService,
    private router: Router,
    public langService: LanguageService,
  ) {
    this.salonForm = this.fb.group({
      name:              ['', Validators.required],
      phone:             ['', phoneValidator],
      notificationPhone: ['', phoneValidator],
      address:           [''],
      logoUrl:           [''],
      themeColor:        ['#7c3aed'],
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
          });
          if (res.data.logoUrl) this.logoPreview = res.data.logoUrl;
        }
        this.isLoading = false;
      },
      error: () => { this.isLoading = false; },
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
