import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors
} from '@angular/forms';
import { Router, ActivatedRoute, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { LogoComponent } from '../../../shared/components/logo/logo.component';

function passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
  const password = control.get('password');
  const confirm  = control.get('confirmPassword');
  if (!password || !confirm) return null;
  return password.value !== confirm.value ? { passwordMismatch: true } : null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule, LogoComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent implements OnInit {
  step = 1;
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  selectedPlan = '';

  step1Form: FormGroup;
  step2Form: FormGroup;

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
  ) {
    this.step1Form = this.fb.group({
      tenantName: ['', [Validators.required, Validators.maxLength(100)]],
      subdomain:  ['', [
        Validators.required,
        Validators.maxLength(50),
        Validators.pattern(/^[a-z0-9-]+$/),
      ]],
      phone: ['', [
        Validators.required,
        Validators.pattern(/^[0-9\+\-\s]{10,15}$/),
      ]],
    });

    this.step2Form = this.fb.group({
      firstName: ['', [Validators.required, Validators.maxLength(50)]],
      lastName:  ['', [Validators.required, Validators.maxLength(50)]],
      email:     ['', [Validators.required, Validators.email]],
      password:        ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.pattern(/(?=.*[A-Z])(?=.*[0-9])/),
      ]],
      confirmPassword: ['', Validators.required],
    }, { validators: passwordMatchValidator });
  }

  ngOnInit(): void {
    this.selectedPlan = this.route.snapshot.queryParamMap.get('plan') || '';
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  onTenantNameChange(): void {
    const name: string = this.step1Form.get('tenantName')?.value || '';
    const slug = name
      .toLowerCase()
      .replace(/ğ/g, 'g').replace(/ü/g, 'u').replace(/ş/g, 's')
      .replace(/ı/g, 'i').replace(/ö/g, 'o').replace(/ç/g, 'c')
      .replace(/[^a-z0-9\s-]/g, '')
      .trim()
      .replace(/\s+/g, '-')
      .replace(/-+/g, '-')
      .slice(0, 50);
    this.step1Form.get('subdomain')?.setValue(slug, { emitEvent: false });
  }

  nextStep(): void {
    if (this.step1Form.invalid) {
      this.step1Form.markAllAsTouched();
      return;
    }
    this.step = 2;
    this.errorMessage = '';
  }

  prevStep(): void {
    this.step = 1;
    this.errorMessage = '';
  }

  onSubmit(): void {
    if (this.step2Form.invalid) {
      this.step2Form.markAllAsTouched();
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    const payload = {
      tenantName: this.step1Form.value.tenantName,
      subdomain:  this.step1Form.value.subdomain,
      phone:      this.step1Form.value.phone,
      firstName:  this.step2Form.value.firstName,
      lastName:   this.step2Form.value.lastName,
      email:      this.step2Form.value.email,
      password:   this.step2Form.value.password,
    };

    this.authService.register(payload).subscribe({
      next: (res) => {
        if (res.success) {
          this.router.navigate(['/dashboard']);
        } else {
          this.errorMessage = res.message || 'Kayıt oluşturulamadı.';
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.status < 500 && err.error?.message
          ? err.error.message
          : 'Bir hata oluştu. Lütfen tekrar deneyin.';
        this.isLoading = false;
      },
    });
  }

  get planLabel(): string {
    const map: Record<string, string> = {
      baslangic:    '🌱 Başlangıç',
      profesyonel:  '⚡ Profesyonel',
      premium:      '👑 Premium',
    };
    return map[this.selectedPlan] || '';
  }
}
