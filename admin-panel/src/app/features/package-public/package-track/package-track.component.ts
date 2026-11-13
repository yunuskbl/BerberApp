import { Component, OnInit, computed, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { PackageService } from '../../../core/services/package.service';
import { PAYMENT_METHOD_LABELS, PublicPackage } from '../../../core/models/package.model';
import { LogoComponent } from '../../../shared/components/logo/logo.component';
import { dateTime, money, shortDate } from '../../packages/package-format';

/**
 * Müşterinin paket takip sayfası — giriş istemez, işletmenin verdiği QR /
 * bağlantıyla açılır. Kalan seans, onaylı seanslar ve kalan borç burada.
 */
@Component({
  selector: 'app-package-track',
  standalone: true,
  imports: [RouterLink, LogoComponent],
  templateUrl: './package-track.component.html',
  styleUrl: './package-track.component.scss',
})
export class PackageTrackComponent implements OnInit {
  readonly data    = signal<PublicPackage | null>(null);
  readonly loading = signal(true);
  readonly error   = signal('');

  readonly methodLabel = PAYMENT_METHOD_LABELS;
  readonly money = money;
  readonly shortDate = shortDate;
  readonly dateTime = dateTime;

  /** Halka grafiği: kullanılan seans oranı (0–1). */
  readonly ratio = computed(() => {
    const p = this.data()?.package;
    return p && p.totalSessions ? Math.min(1, p.usedSessions / p.totalSessions) : 0;
  });
  readonly circumference = 2 * Math.PI * 52;

  constructor(private route: ActivatedRoute, private packages: PackageService) {}

  ngOnInit(): void {
    const token = this.route.snapshot.paramMap.get('token') ?? '';
    this.packages.getPublicPackage(token).subscribe({
      next: r => { this.loading.set(false); if (r.success) this.data.set(r.data); },
      error: err => {
        this.loading.set(false);
        this.error.set(err?.status === 404
          ? 'Paket bulunamadı. Bağlantıyı salonunuzdan yeniden isteyin.'
          : 'Bilgiler şu an yüklenemedi. Lütfen biraz sonra tekrar deneyin.');
      },
    });
  }

  telHref(phone?: string | null): string {
    return 'tel:' + (phone ?? '').replace(/[^\d+]/g, '');
  }
}
