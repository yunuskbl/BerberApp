import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  FormBuilder, FormGroup, Validators, ReactiveFormsModule, AbstractControl, ValidationErrors, FormsModule
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

const DISPOSABLE_EMAIL_DOMAINS = [
  'mailinator.com', 'guerrillamail.com', 'guerrillamail.net', 'sharklasers.com',
  '10minutemail.com', '10minutemail.net', 'temp-mail.org', 'tempmail.com',
  'tempmail.dev', 'tempmailo.com', 'yopmail.com', 'trashmail.com',
  'getnada.com', 'dispostable.com', 'maildrop.cc', 'mintemail.com',
  'throwawaymail.com', 'fakeinbox.com', 'mohmal.com', 'emailondeck.com',
  'mailnesia.com', 'mytemp.email', 'burnermail.io', 'mailsac.com',
  'inboxkitten.com', 'spamgourmet.com', 'mailpoof.com', 'tmpmail.org',
];

/** Tek kullanımlık mail servisleri engelli — kurumsal domain'ler serbest */
function trustedEmailDomainValidator(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value || '';
  const at = value.lastIndexOf('@');
  if (at < 0 || at === value.length - 1) return null; // format hatasını Validators.email yakalar
  const domain = value.slice(at + 1).trim().toLowerCase();
  return DISPOSABLE_EMAIL_DOMAINS.includes(domain) ? { untrustedDomain: true } : null;
}

/** Rastgele karakter dizisi tespiti: en az bir sesli harf, 5+ ardışık sessiz harf yok */
function plausibleEmailValidator(control: AbstractControl): ValidationErrors | null {
  const value: string = control.value || '';
  const at = value.indexOf('@');
  if (at <= 0) return null;
  const local = value.slice(0, at).split('+')[0].toLowerCase();
  if (local.length < 3) return { implausibleEmail: true };

  const letters = local.replace(/[^a-z]/g, '');
  if (!letters) return { implausibleEmail: true };
  if (!/[aeiou]/.test(letters)) return { implausibleEmail: true };
  if (/[bcdfghjklmnpqrstvwxyz]{5,}/.test(local)) return { implausibleEmail: true };
  return null;
}

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, RouterModule, LogoComponent],
  templateUrl: './register.component.html',
  styleUrl: './register.component.scss',
})
export class RegisterComponent implements OnInit, OnDestroy {
  step = 1;
  isLoading = false;
  errorMessage = '';
  showPassword = false;
  selectedPlan = '';
  buyMode = false;

  // OTP state
  otpSent = false;
  otpVerified = false;
  otpCode = '';
  isSendingOtp = false;
  isVerifyingOtp = false;
  otpError = '';
  otpSuccess = '';
  otpTimer = 0;
  private timerInterval: any;

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
      email:     ['', [Validators.required, Validators.email, trustedEmailDomainValidator, plausibleEmailValidator]],
      password:        ['', [
        Validators.required,
        Validators.minLength(6),
        Validators.pattern(/(?=.*[A-ZÇĞİÖŞÜ])(?=.*[0-9])/),
      ]],
      confirmPassword: ['', Validators.required],
    }, { validators: passwordMatchValidator });
  }

  ngOnInit(): void {
    this.selectedPlan = this.route.snapshot.queryParamMap.get('plan') || '';
    this.buyMode = this.route.snapshot.queryParamMap.get('mode') === 'buy';
    if (this.authService.isLoggedIn()) {
      this.router.navigate(['/dashboard']);
    }
  }

  ngOnDestroy(): void {
    if (this.timerInterval) clearInterval(this.timerInterval);
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
    if (this.step === 1) {
      if (this.step1Form.invalid) { this.step1Form.markAllAsTouched(); return; }
      this.step = 2;
    } else if (this.step === 2) {
      if (this.step2Form.invalid) { this.step2Form.markAllAsTouched(); return; }
      this.step = 3;
      // OTP durumunu sıfırla (kullanıcı adım 2'ye geri dönüp ilerlerse)
      this.otpSent = false;
      this.otpVerified = false;
      this.otpCode = '';
      this.otpError = '';
      this.otpSuccess = '';
    }
    this.errorMessage = '';
  }

  prevStep(): void {
    this.step = this.step > 1 ? this.step - 1 : 1;
    this.errorMessage = '';
  }

  sendOtp(): void {
    const phone = this.step1Form.get('phone')?.value;
    if (!phone) return;

    this.isSendingOtp = true;
    this.otpError = '';
    this.otpSuccess = '';

    const email = this.step2Form.get('email')?.value;
    this.authService.sendRegistrationOtp(phone, email).subscribe({
      next: (res) => {
        if (res.success) {
          this.otpSent = true;
          this.otpSuccess = 'Doğrulama kodu e-posta adresinize gönderildi.';
          this.startTimer();
        } else {
          this.otpError = res.message || 'Kod gönderilemedi.';
        }
        this.isSendingOtp = false;
      },
      error: (err) => {
        this.otpError = err.status < 500 && err.error?.message
          ? err.error.message : 'Bir hata oluştu. Lütfen tekrar deneyin.';
        this.isSendingOtp = false;
      },
    });
  }

  verifyOtp(): void {
    const phone = this.step1Form.get('phone')?.value;
    if (!phone || !this.otpCode) return;

    this.isVerifyingOtp = true;
    this.otpError = '';

    this.authService.verifyRegistrationOtp(phone, this.otpCode).subscribe({
      next: (res) => {
        if (res.success) {
          this.otpVerified = true;
          this.otpSuccess = 'Telefon numaranız doğrulandı! ✓';
          clearInterval(this.timerInterval);
        } else {
          this.otpError = res.message || 'Kod hatalı.';
        }
        this.isVerifyingOtp = false;
      },
      error: (err) => {
        this.otpError = err.status < 500 && err.error?.message
          ? err.error.message : 'Bir hata oluştu. Lütfen tekrar deneyin.';
        this.isVerifyingOtp = false;
      },
    });
  }

  startTimer(): void {
    if (this.timerInterval) clearInterval(this.timerInterval);
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

  onSubmit(): void {
    if (!this.otpVerified) {
      this.errorMessage = 'Lütfen telefon numaranızı doğrulayın.';
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
          if (this.buyMode && this.selectedPlan) {
            this.router.navigate(['/payment'], { queryParams: { plan: this.selectedPlan } });
          } else {
            this.router.navigate(['/dashboard']);
          }
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
