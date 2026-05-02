import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder,
  FormGroup,
  Validators,
  ReactiveFormsModule,
  AbstractControl,
  ValidationErrors,
} from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { LanguageService } from '../../../core/services/language.service';
import { TranslatePipe } from '../../../shared/pipes/translate.pipe';

/* Turkish phone validator */
function phoneValidator(ctrl: AbstractControl): ValidationErrors | null {
  const val: string = (ctrl.value || '').replace(/\s/g, '');
  if (!val) return null;
  const ok = /^(\+?[0-9]{7,15})$/.test(val);
  return ok ? null : { invalidPhone: true };
}

/* OTP validator - exactly 6 digits */
function otpValidator(ctrl: AbstractControl): ValidationErrors | null {
  const val = (ctrl.value || '').replace(/\s/g, '');
  if (!val) return null;
  const ok = /^[0-9]{6}$/.test(val);
  return ok ? null : { invalidOtp: true };
}

/* Password strength validators */
function uppercaseValidator(ctrl: AbstractControl): ValidationErrors | null {
  return /[A-Z]/.test(ctrl.value || '') ? null : { noUppercase: true };
}
function numberValidator(ctrl: AbstractControl): ValidationErrors | null {
  return /[0-9]/.test(ctrl.value || '') ? null : { noNumber: true };
}

@Component({
  selector: 'app-forgot-password',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, TranslatePipe],
  templateUrl: './forgot-password.component.html',
  styleUrl: './forgot-password.component.scss',
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
    private router: Router,
    public langService: LanguageService,
  ) {
    this.phoneForm = this.fb.group({
      phone: ['', [Validators.required, phoneValidator]],
    });

    this.resetForm = this.fb.group(
      {
        otp: ['', [Validators.required, otpValidator]],
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
        this.isLoading = false;
        this.step = 'reset';
      },
    });
  }

  resetPassword(): void {
    if (this.resetForm.invalid) return;

    const { otp, newPassword } = this.resetForm.value;

    this.isLoading = true;
    this.errorMessage = '';

    this.http
      .post('/api/auth/reset-password', {
        phone: this.phone,
        otp,
        newPassword,
      })
      .subscribe({
        next: () => {
          this.isLoading = false;
          this.successMessage = this.langService.t(
            'settings.password.changed',
          );
          setTimeout(() => this.router.navigate(['/login']), 2000);
        },
        error: (err) => {
          this.errorMessage = err.error?.message || 'Bir hata oluştu. Tekrar deneyin.';
          this.isLoading = false;
        },
      });
  }

  passwordMatchValidator(form: FormGroup) {
    const np = form.get('newPassword')?.value;
    const cp = form.get('confirmPassword')?.value;
    return np === cp ? null : { passwordMismatch: true };
  }

  /* Password rule getters */
  get newPassValue(): string {
    return this.resetForm.get('newPassword')?.value || '';
  }
  get passRuleMinLen(): boolean {
    return this.newPassValue.length >= 8;
  }
  get passRuleUpper(): boolean {
    return /[A-Z]/.test(this.newPassValue);
  }
  get passRuleNumber(): boolean {
    return /[0-9]/.test(this.newPassValue);
  }
  get newPassTouched(): boolean {
    return !!this.resetForm.get('newPassword')?.touched;
  }

  /* OTP input formatting */
  onOtpInput(event: any): void {
    let value = event.target.value.replace(/\D/g, '').slice(0, 6);
    this.resetForm.get('otp')?.setValue(value);
  }
}
