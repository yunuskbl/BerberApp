import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, Router, RouterModule } from '@angular/router';
import { HttpClient } from '@angular/common/http';
import { environment } from '../../../../environments/environment';
import { LogoComponent } from '../../../shared/components/logo/logo.component';

@Component({
  selector: 'app-verify-email',
  standalone: true,
  imports: [CommonModule, RouterModule, LogoComponent],
  template: `
    <div class="verify-wrapper">
      <div class="verify-card">
        <div class="verify-logo">
          <app-logo variant="light"></app-logo>
        </div>

        @if (state === 'loading') {
          <div class="state-icon spin">⏳</div>
          <h2>Doğrulanıyor...</h2>
          <p class="sub">E-posta adresiniz doğrulanıyor, lütfen bekleyin.</p>
        }

        @if (state === 'success') {
          <div class="state-icon">✅</div>
          <h2>E-posta Doğrulandı!</h2>
          <p class="sub">Harika! E-posta adresiniz başarıyla doğrulandı. Artık tüm özelliklere erişebilirsiniz.</p>
          <a routerLink="/dashboard" class="btn-primary">Panele Git</a>
        }

        @if (state === 'error') {
          <div class="state-icon">❌</div>
          <h2>Bağlantı Geçersiz</h2>
          <p class="sub">Bu doğrulama bağlantısı geçersiz veya süresi dolmuş. Yeni bir bağlantı talep edebilirsiniz.</p>
          <a routerLink="/dashboard" class="btn-primary">Panele Git</a>
        }
      </div>
    </div>
  `,
  styles: [`
    .verify-wrapper {
      min-height: 100vh;
      display: flex;
      align-items: center;
      justify-content: center;
      background: #111827;
      padding: 24px 16px;
    }
    .verify-card {
      width: 100%;
      max-width: 420px;
      background: #fff;
      border-radius: 20px;
      padding: 48px 40px;
      box-shadow: 0 32px 64px rgba(0,0,0,0.4);
      text-align: center;
    }
    .verify-logo {
      display: flex;
      justify-content: center;
      margin-bottom: 32px;
      ::ng-deep .wordmark { font-size: 28px !important; color: #111827 !important; }
      ::ng-deep .wordmark::after { background: #111827 !important; }
      ::ng-deep .tagline { color: #9ca3af !important; font-size: 9px !important; }
    }
    .state-icon {
      font-size: 48px;
      margin-bottom: 16px;
      display: block;
      &.spin { animation: pulse 1.5s ease-in-out infinite; }
    }
    h2 {
      font-size: 22px;
      font-weight: 700;
      color: #111827;
      margin: 0 0 12px;
    }
    .sub {
      font-size: 14px;
      color: #6b7280;
      line-height: 1.6;
      margin: 0 0 28px;
    }
    .btn-primary {
      display: inline-block;
      padding: 13px 32px;
      background: #111827;
      color: #fff;
      border-radius: 10px;
      font-weight: 700;
      font-size: 15px;
      text-decoration: none;
      transition: all 0.18s;
      &:hover { background: #1f2937; transform: translateY(-1px); }
    }
    @keyframes pulse {
      0%, 100% { opacity: 1; }
      50% { opacity: 0.5; }
    }
  `]
})
export class VerifyEmailComponent implements OnInit {
  state: 'loading' | 'success' | 'error' = 'loading';

  constructor(
    private route: ActivatedRoute,
    private http: HttpClient,
  ) {}

  ngOnInit(): void {
    const token = this.route.snapshot.queryParamMap.get('token');
    if (!token) {
      this.state = 'error';
      return;
    }

    this.http.get<any>(`${environment.apiUrl}/auth/verify-email?token=${encodeURIComponent(token)}`)
      .subscribe({
        next: (res) => {
          this.state = res.success ? 'success' : 'error';
        },
        error: () => {
          this.state = 'error';
        }
      });
  }
}
