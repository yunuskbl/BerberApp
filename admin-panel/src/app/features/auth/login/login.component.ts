import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterModule],
  templateUrl: './login.component.html',
  styleUrl: './login.component.scss'
})
export class LoginComponent {
  loginForm: FormGroup;
  isLoading = false;
  errorMessage = '';

  constructor(
    private fb: FormBuilder,
    private authService: AuthService,
    private router: Router
  ) {
    this.loginForm = this.fb.group({
      email:    ['', [Validators.required, Validators.email]],
      password: ['', Validators.required]
    });
  }

  onSubmit(): void {
    if (this.loginForm.invalid) return;

    this.isLoading     = true;
    this.errorMessage  = '';

    this.authService.login(this.loginForm.value).subscribe({
      next: (response) => {
        if (response.success) {
          // SuperAdmin'i ayrı panele yönlendir
          if (this.authService.isSuperAdmin()) {
            this.router.navigate(['/superadmin']);
          } else {
            this.router.navigate(['/dashboard']);
          }
        } else {
          this.errorMessage = response.message;
        }
        this.isLoading = false;
      },
      error: (err) => {
        this.errorMessage = err.error?.message || 'Giriş yapılırken hata oluştu.';
        this.isLoading = false;
      }
    });
  }
}