import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  ReactiveFormsModule,
  FormBuilder,
  FormGroup,
  Validators,
} from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../environments/environment';
import { AuthService } from '../../core/services/auth.service';
import { Router, RouterModule } from '@angular/router';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
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

  salonForm: FormGroup;
  passwordForm: FormGroup;

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private authService: AuthService,
    private router: Router
  ) {
    this.salonForm = this.fb.group({
      name: ['', Validators.required],
      phone: [''],
      notificationPhone: [''],
      address: [''],
      logoUrl: [''],
    });

    this.passwordForm = this.fb.group(
      {
        currentPassword: ['', Validators.required],
        newPassword: ['', [Validators.required, Validators.minLength(6)]],
        confirmPassword: ['', Validators.required],
      },
      { validators: this.passwordMatchValidator },
    );
  }

  ngOnInit(): void {
    this.loadSalonInfo();
  }

  loadSalonInfo(): void {
    this.http.get<any>(`${environment.apiUrl}/tenants/me`).subscribe({
      next: (res) => {
        if (res.success) {
          this.salonForm.patchValue({
            name: res.data.name,
            phone: res.data.phone,
            notificationPhone: res.data.notificationPhone,
            address: res.data.address,
            logoUrl: res.data.logoUrl ?? '',
          });
          if (res.data.logoUrl) {
            this.logoPreview = res.data.logoUrl;
          }
        }
        this.isLoading = false;
      },
      error: () => {
        this.isLoading = false;
      },
    });
  }

  onSaveSalon(): void {
    if (this.salonForm.invalid) return;
    this.isSaving = true;
    this.successMessage = '';
    this.errorMessage = '';

    this.http
      .put<any>(`${environment.apiUrl}/tenants`, this.salonForm.value)
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.successMessage = 'Salon bilgileri güncellendi!';
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

  onChangePassword(): void {
    if (this.passwordForm.invalid) return;
    this.isChangingPass = true;
    this.passSuccess = '';
    this.passError = '';

    const { currentPassword, newPassword } = this.passwordForm.value;

    this.http
      .post<any>(`${environment.apiUrl}/auth/change-password`, {
        currentPassword,
        newPassword,
      })
      .subscribe({
        next: (res) => {
          if (res.success) {
            this.passSuccess = 'Şifre başarıyla değiştirildi!';
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
    const newPass = form.get('newPassword')?.value;
    const confirmPass = form.get('confirmPassword')?.value;
    return newPass === confirmPass ? null : { passwordMismatch: true };
  }

  get user() {
    return this.authService.getUser();
  }
  get bookingUrl(): string {
    const subdomain = this.authService.getUser()?.subdomain;
    return subdomain ? `${window.location.origin}/book/${subdomain}` : '';
  }

  onLogoFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    if (!input.files?.length) return;

    const file = input.files[0];
    this.logoUploadError = '';
    this.isUploadingLogo = true;

    // Anlık önizleme
    const reader = new FileReader();
    reader.onload = (e) => {
      this.logoPreview = e.target?.result as string;
    };
    reader.readAsDataURL(file);

    // API'ye yükle
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

    // Aynı dosyayı tekrar seçebilmek için input'u sıfırla
    input.value = '';
  }

  copied = false; // salon bilgileri kopyala
  copiedMain = false; // müşteri randevu linki kopyala

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