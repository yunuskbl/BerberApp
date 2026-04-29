import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss'
})
export class ForgotPasswordComponent {
  step: 'phone' | 'reset' = 'phone';

  phoneForm: FormGroup;
  resetForm: FormGroup;

  isLoading = false;
  errorMessage = '';
  successMessage = '';

  private phone = '';

  constructor(
    private fb: FormBuilder,
    private http: HttpClient,
    private router: Router
  ) {
    this.phoneForm = this.fb.group({
      phone: ['', [Validators.required, Validators.pattern(/^[0-9]{10,11}$/)]]
    });

    this.resetForm = this.fb.group({
      otp: ['', [Validators.required, Validators.pattern(/^[0-9]{6}$/)]],
      newPassword: ['', [Validators.required, Validators.minLength(6)]],
      confirmPassword: ['', Validators.required]
    });
  }

  sendOtp(): void {
    if (this.phoneForm.invalid) return;

    this.isLoading = true;
    this.errorMessage = '';

    this.phone = this.phoneForm.value.phone;

    this.http.post('/api/auth/forgot-password', { phone: this.phone }).subscribe({
      next: () => {
        this.isLoading = false;
        this.step = 'reset';
      },
      error: () => {
        // Güvenlik: hata olsa bile aynı mesajı göster
        this.isLoading = false;
        this.step = 'reset';
      }
    });
  }

  resetPassword(): void {
    if (this.resetForm.invalid) return;

    const { otp, newPassword, confirmPassword } = this.resetForm.value;

    if (newPassword !== confirmPassword) {
      this.errorMessage = 'Şifreler eşleşmiyor.';
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.http.post('/api/auth/reset-password', {
      phone: this.phone,
      otp,
      newPassword
    }).subscribe({
      next: () => {
        this.isLoading = false;
        this.successMessage = 'Şifreniz güncellendi. Giriş yapabilirsiniz.';
        setTimeout(() => this.router.navigate(['/login']), 2000);
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Bir hata oluştu. Tekrar deneyin.';
        this.isLoading = false;
      }
    });
  }
}
