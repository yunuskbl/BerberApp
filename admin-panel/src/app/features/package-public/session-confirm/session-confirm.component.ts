import { Component, OnInit, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PackageService } from '../../../core/services/package.service';
import { PublicSessionInfo } from '../../../core/models/package.model';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { dateTime } from '../../packages/package-format';

/**
 * Seans onayı — müşteri salonda ekranda gösterilen QR'ı okutunca açılır.
 * Seans zaten işlenmiş durumda; müşteri yalnızca "bu seansı aldım" der.
 */
@Component({
  selector: 'app-session-confirm',
  standalone: true,
  imports: [RouterLink, LogoComponent],
  templateUrl: './session-confirm.component.html',
  styleUrl: './session-confirm.component.scss',
})
export class SessionConfirmComponent implements OnInit {
  readonly info       = signal<PublicSessionInfo | null>(null);
  readonly loading    = signal(true);
  readonly confirming = signal(false);
  readonly error      = signal('');
  readonly justConfirmed = signal(false);

  readonly dateTime = dateTime;
  private code = '';

  constructor(private route: ActivatedRoute, private packages: PackageService) {}

  ngOnInit(): void {
    this.code = this.route.snapshot.paramMap.get('code') ?? '';
    this.packages.getPublicSession(this.code).subscribe({
      next: r => { this.loading.set(false); if (r.success) this.info.set(r.data); },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.status === 404
          ? 'Bu onay kodu geçersiz. Personelden QR\'ı yeniden göstermesini isteyin.'
          : 'Şu an bağlanılamadı. Lütfen tekrar deneyin.');
      },
    });
  }

  confirm(): void {
    if (this.confirming()) return;
    this.confirming.set(true);
    this.error.set('');
    this.packages.confirmPublicSession(this.code).subscribe({
      next: r => {
        this.confirming.set(false);
        if (r.success) { this.info.set(r.data); this.justConfirmed.set(true); }
      },
      error: err => {
        this.confirming.set(false);
        this.error.set(err?.error?.message || 'Onaylanamadı. Lütfen tekrar deneyin.');
        // Süresi dolduysa ya da iptal edildiyse güncel durumu göster.
        this.packages.getPublicSession(this.code).subscribe({ next: r => r.success && this.info.set(r.data), error: () => {} });
      },
    });
  }
}
