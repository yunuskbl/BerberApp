import { Component, OnInit, computed, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { PackageService } from '../../../core/services/package.service';
import { PackageDetail, PackageSummary, PackageTemplate } from '../../../core/models/package.model';
import { PackageDetailComponent } from '../package-detail/package-detail.component';
import { PackageTemplatesComponent } from '../package-templates/package-templates.component';
import { SellPackageComponent } from '../sell-package/sell-package.component';
import { money, relativeDay, statusLabel } from '../package-format';

type StatusFilter = 'active' | 'completed' | 'cancelled' | 'all';

/**
 * Seans paketleri: müşterilere satılan paketlerin listesi, satış, ayrıntı
 * paneli ve paket tanımları.
 */
@Component({
  selector: 'app-packages-page',
  standalone: true,
  imports: [FormsModule, PackageDetailComponent, PackageTemplatesComponent, SellPackageComponent],
  templateUrl: './packages-page.component.html',
  styleUrl: './packages-page.component.scss',
})
export class PackagesPageComponent implements OnInit {
  readonly tab       = signal<'packages' | 'templates'>('packages');
  readonly status    = signal<StatusFilter>('active');
  readonly packages  = signal<PackageSummary[]>([]);
  readonly templates = signal<PackageTemplate[]>([]);
  readonly loading   = signal(true);
  readonly error     = signal('');
  readonly query     = signal('');

  readonly selectedId = signal<string | null>(null);
  /** Randevu tamamlandıktan sonra gelindiyse açılınca QR'ı gösterilecek seans. */
  readonly qrSessionId = signal<string | null>(null);
  readonly selling    = signal(false);

  readonly filters: { key: StatusFilter; label: string }[] = [
    { key: 'active', label: 'Aktif' },
    { key: 'completed', label: 'Tamamlanan' },
    { key: 'cancelled', label: 'İptal' },
    { key: 'all', label: 'Tümü' },
  ];

  readonly visible = computed(() => {
    const q = this.query().trim().toLocaleLowerCase('tr-TR');
    if (!q) return this.packages();
    return this.packages().filter(p =>
      p.customerName.toLocaleLowerCase('tr-TR').includes(q) ||
      p.name.toLocaleLowerCase('tr-TR').includes(q) ||
      (p.customerPhone ?? '').includes(q));
  });

  /** Aktif paketlerdeki toplam alacak — üstte özet olarak. */
  readonly totalDebt = computed(() => this.packages().reduce((s, p) => s + (p.status !== 'Cancelled' ? p.remainingDebt : 0), 0));

  readonly money = money;
  readonly relativeDay = relativeDay;
  readonly statusLabel = statusLabel;

  constructor(private service: PackageService, private route: ActivatedRoute, private router: Router) {}

  ngOnInit(): void {
    this.load();
    this.service.getTemplates().subscribe({
      next: r => { if (r.success) this.templates.set(r.data ?? []); },
      error: () => {},
    });
    // ?id=… ile doğrudan bir paketi aç (ör. müşteri sayfasından gelindiğinde).
    const id = this.route.snapshot.queryParamMap.get('id');
    if (id) this.selectedId.set(id);
    this.qrSessionId.set(this.route.snapshot.queryParamMap.get('qr'));
  }

  load(): void {
    this.loading.set(true);
    this.error.set('');
    this.service.getPackages(this.status()).subscribe({
      next: r => { this.loading.set(false); if (r.success) this.packages.set(r.data ?? []); },
      error: err => { this.loading.set(false); this.error.set(err?.error?.message || 'Paketler yüklenemedi.'); },
    });
  }

  setStatus(s: StatusFilter): void {
    if (this.status() === s) return;
    this.status.set(s);
    this.load();
  }

  open(p: PackageSummary): void { this.qrSessionId.set(null); this.selectedId.set(p.id); }

  closeDetail(): void {
    this.selectedId.set(null);
    this.qrSessionId.set(null);
    if (this.route.snapshot.queryParamMap.has('id')) {
      this.router.navigate([], { queryParams: { id: null, qr: null }, queryParamsHandling: 'merge', replaceUrl: true });
    }
  }

  /** Ayrıntıdaki değişikliği listeye yansıt (seans, ödeme, iptal). */
  onChanged(summary: PackageSummary): void {
    this.packages.update(list => {
      const i = list.findIndex(p => p.id === summary.id);
      if (i < 0) return list;
      const next = [...list];
      next[i] = summary;
      return next;
    });
  }

  onSold(detail: PackageDetail): void {
    this.selling.set(false);
    this.tab.set('packages');
    if (this.status() !== 'active' && this.status() !== 'all') this.status.set('active');
    this.load();
    this.selectedId.set(detail.package.id);
  }

  percent(p: PackageSummary): number {
    return p.totalSessions ? Math.round((p.usedSessions / p.totalSessions) * 100) : 0;
  }
}
