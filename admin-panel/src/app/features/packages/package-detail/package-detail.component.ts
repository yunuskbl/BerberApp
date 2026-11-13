import { Component, OnDestroy, OnInit, computed, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import QRCode from 'qrcode';
import { PackageService } from '../../../core/services/package.service';
import { StaffService } from '../../../core/services/staff.service';
import { Staff } from '../../../core/models/staff.model';
import {
  PAYMENT_METHOD_LABELS, PackageDetail, PackagePaymentMethod, PackageSession, PackageSummary,
  SessionConfirm, packageTrackUrl, sessionConfirmUrl,
} from '../../../core/models/package.model';
import { dateTime, money, relativeDay, shortDate, statusLabel } from '../package-format';

/** Seans onay QR penceresinin durumu. */
interface ConfirmDialog {
  confirm: SessionConfirm;
  sessionNumber: number | null;
  qr: string;
  state: 'waiting' | 'confirmed' | 'expired';
}

/**
 * Bir paketin ayrıntısı: ilerleme, seans işleme (müşteri onay QR'ı ile),
 * ödemeler, müşteri takip bağlantısı, iptaller.
 */
@Component({
  selector: 'app-package-detail',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './package-detail.component.html',
  styleUrl: './package-detail.component.scss',
})
export class PackageDetailComponent implements OnInit, OnDestroy {
  readonly packageId = input.required<string>();
  /** Açılınca bu seansın müşteri onay QR'ını göster (randevu tamamlandıktan sonra). */
  readonly autoQrSessionId = input<string | null>(null);
  private autoQrShown = false;
  /** Liste güncel özeti göstersin diye. */
  readonly changed = output<PackageSummary>();
  readonly closed  = output<void>();

  readonly detail  = signal<PackageDetail | null>(null);
  readonly staff   = signal<Staff[]>([]);
  readonly loading = signal(true);
  readonly busy    = signal(false);
  readonly error   = signal('');
  readonly notice  = signal('');

  // Seans işleme formu
  sessionStaffId = '';
  sessionNote = '';

  // Ödeme formu
  paymentOpen = false;
  paymentAmount: number | null = null;
  paymentMethod: PackagePaymentMethod = 'Cash';
  paymentNote = '';

  // Müşteri kartı
  readonly trackQr = signal('');
  readonly copied  = signal(false);

  // Onay QR penceresi
  readonly dialog    = signal<ConfirmDialog | null>(null);
  readonly countdown = signal('');
  private pollTimer: ReturnType<typeof setInterval> | null = null;
  private tickTimer: ReturnType<typeof setInterval> | null = null;

  readonly methods = Object.entries(PAYMENT_METHOD_LABELS) as [PackagePaymentMethod, string][];
  readonly methodLabel = PAYMENT_METHOD_LABELS;
  readonly money = money;
  readonly shortDate = shortDate;
  readonly dateTime = dateTime;
  readonly relativeDay = relativeDay;
  readonly statusLabel = statusLabel;

  readonly pkg = computed(() => this.detail()?.package ?? null);
  readonly canRecord = computed(() => {
    const p = this.pkg();
    return !!p && p.status !== 'Cancelled' && p.remainingSessions > 0;
  });
  /** İlerleme noktaları: kullanılan seans dolu, müşterinin onayladığı işaretli. */
  readonly dots = computed(() => {
    const d = this.detail();
    if (!d || d.package.totalSessions > 30) return [];
    const valid = d.sessions.filter(s => !s.isVoid).sort((a, b) => (a.number ?? 0) - (b.number ?? 0));
    return Array.from({ length: d.package.totalSessions }, (_, i) => {
      const s = valid[i];
      return { used: !!s, confirmed: !!s?.customerConfirmedAt, n: i + 1 };
    });
  });

  constructor(private packages: PackageService, private staffService: StaffService) {}

  ngOnInit(): void {
    this.load();
    this.staffService.getAll().subscribe({
      next: r => { if (r.success) this.staff.set((r.data ?? []).filter(s => s.isActive)); },
      error: () => {},
    });
  }

  ngOnDestroy(): void { this.stopTimers(); }

  load(after?: (d: PackageDetail) => void): void {
    this.packages.getPackage(this.packageId()).subscribe({
      next: r => {
        this.loading.set(false);
        if (!r.success) return;
        this.apply(r.data);
        after?.(r.data);
        this.maybeAutoQr(r.data);
      },
      error: err => { this.loading.set(false); this.error.set(err?.error?.message || 'Paket yüklenemedi.'); },
    });
  }

  private apply(d: PackageDetail): void {
    this.detail.set(d);
    this.changed.emit(d.package);
    // Son seansı yapan personel varsayılan olsun: paketler çoğunlukla aynı kişide.
    if (!this.sessionStaffId) {
      const last = d.sessions.find(s => !s.isVoid && s.staffId);
      if (last?.staffId) this.sessionStaffId = last.staffId;
    }
    QRCode.toDataURL(packageTrackUrl(d.package.publicToken), { margin: 1, width: 220 })
      .then(url => this.trackQr.set(url))
      .catch(() => this.trackQr.set(''));
  }

  private maybeAutoQr(d: PackageDetail): void {
    const id = this.autoQrSessionId();
    if (!id || this.autoQrShown) return;
    this.autoQrShown = true;
    const s = d.sessions.find(x => x.id === id);
    if (s && !s.isVoid && !s.customerConfirmedAt) this.showQrFor(s);
  }

  // ─── Seans ───────────────────────────────────────────────

  recordSession(): void {
    const p = this.pkg();
    if (!p || this.busy()) return;
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    this.packages.recordSession(p.id, this.sessionStaffId || null, this.sessionNote.trim() || null).subscribe({
      next: r => {
        this.busy.set(false);
        if (!r.success) return;
        this.sessionNote = '';
        this.apply(r.data);
        if (r.data.confirm) {
          const number = r.data.sessions.find(s => s.id === r.data.confirm!.sessionId)?.number ?? null;
          this.openDialog(r.data.confirm, number);
        }
      },
      error: err => { this.busy.set(false); this.error.set(err?.error?.message || 'Seans işlenemedi.'); },
    });
  }

  /** Onaylanmamış eski bir seans için yeniden QR göster (yeni kod üretir). */
  showQrFor(s: PackageSession): void {
    const p = this.pkg();
    if (!p || this.busy()) return;
    this.busy.set(true);
    this.packages.renewConfirmCode(p.id, s.id).subscribe({
      next: r => { this.busy.set(false); if (r.success) this.openDialog(r.data, s.number); },
      error: err => { this.busy.set(false); this.error.set(err?.error?.message || 'Kod oluşturulamadı.'); },
    });
  }

  voidSession(s: PackageSession): void {
    const p = this.pkg();
    if (!p) return;
    const reason = prompt(`${s.number}. seans iptal edilsin mi? Seans pakete geri eklenir.\nİsterseniz nedenini yazın:`, '');
    if (reason === null) return;
    this.mutate(this.packages.voidSession(p.id, s.id, reason.trim() || null), 'Seans iptal edildi; paket bakiyesine geri eklendi.');
  }

  // ─── Onay QR penceresi ───────────────────────────────────

  private async openDialog(confirm: SessionConfirm, sessionNumber: number | null): Promise<void> {
    this.stopTimers();
    let qr = '';
    try { qr = await QRCode.toDataURL(sessionConfirmUrl(confirm.code), { margin: 1, width: 280 }); } catch { /* yok */ }
    this.dialog.set({ confirm, sessionNumber, qr, state: 'waiting' });
    this.tick();
    this.tickTimer = setInterval(() => this.tick(), 1000);
    this.pollTimer = setInterval(() => this.poll(), 3000);
  }

  private tick(): void {
    const d = this.dialog();
    if (!d || d.state !== 'waiting') return;
    const left = new Date(d.confirm.expiresAt).getTime() - Date.now();
    if (left <= 0) {
      this.dialog.set({ ...d, state: 'expired' });
      this.countdown.set('');
      this.stopTimers();
      return;
    }
    const m = Math.floor(left / 60000), s = Math.floor((left % 60000) / 1000);
    this.countdown.set(`${m}:${s.toString().padStart(2, '0')}`);
  }

  private poll(): void {
    const d = this.dialog(), p = this.pkg();
    if (!d || !p || d.state !== 'waiting') return;
    this.packages.sessionStatus(p.id, d.confirm.sessionId).subscribe({
      next: r => {
        if (!r.success || !r.data.customerConfirmedAt) return;
        const cur = this.dialog();
        if (cur?.confirm.sessionId !== d.confirm.sessionId) return;
        this.dialog.set({ ...cur, state: 'confirmed' });
        this.stopTimers();
        this.load();
      },
      error: () => {},
    });
  }

  renewInDialog(): void {
    const d = this.dialog(), p = this.pkg();
    if (!d || !p) return;
    this.packages.renewConfirmCode(p.id, d.confirm.sessionId).subscribe({
      next: r => { if (r.success) this.openDialog(r.data, d.sessionNumber); },
      error: err => this.error.set(err?.error?.message || 'Kod oluşturulamadı.'),
    });
  }

  closeDialog(): void {
    this.stopTimers();
    this.dialog.set(null);
    this.load();
  }

  private stopTimers(): void {
    if (this.pollTimer) { clearInterval(this.pollTimer); this.pollTimer = null; }
    if (this.tickTimer) { clearInterval(this.tickTimer); this.tickTimer = null; }
  }

  // ─── Ödeme ───────────────────────────────────────────────

  openPayment(): void {
    const p = this.pkg();
    if (!p) return;
    this.paymentOpen = true;
    this.paymentAmount = p.remainingDebt > 0 ? p.remainingDebt : null;
    this.paymentMethod = 'Cash';
    this.paymentNote = '';
  }

  savePayment(): void {
    const p = this.pkg();
    if (!p || this.busy()) return;
    if (!this.paymentAmount || this.paymentAmount <= 0) { this.error.set('Ödeme tutarını girin.'); return; }
    if (this.paymentAmount > p.remainingDebt) { this.error.set(`Ödeme kalan borçtan (${money(p.remainingDebt)}) fazla olamaz.`); return; }
    this.mutate(
      this.packages.addPayment(p.id, this.paymentAmount, this.paymentMethod, this.paymentNote.trim() || null),
      'Ödeme kaydedildi.',
      () => (this.paymentOpen = false),
    );
  }

  voidPayment(id: string, amount: number): void {
    const p = this.pkg();
    if (!p || !confirm(`${money(amount)} tutarındaki ödeme iptal edilsin mi? Borca geri eklenir.`)) return;
    this.mutate(this.packages.voidPayment(p.id, id), 'Ödeme iptal edildi.');
  }

  // ─── Paket ───────────────────────────────────────────────

  cancelPackage(): void {
    const p = this.pkg();
    if (!p) return;
    const reason = prompt('Paket iptal edilsin mi? Bundan sonra seans işlenemez.\nİsterseniz nedenini yazın:', '');
    if (reason === null) return;
    this.mutate(this.packages.cancel(p.id, reason.trim() || null), 'Paket iptal edildi.');
  }

  copyLink(): void {
    const p = this.pkg();
    if (!p) return;
    navigator.clipboard?.writeText(packageTrackUrl(p.publicToken)).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    }).catch(() => {});
  }

  /** Müşterinin WhatsApp'ına takip bağlantısı (işletmenin kendi WhatsApp'ından açılır). */
  whatsAppUrl(): string {
    const p = this.pkg();
    if (!p) return '#';
    let phone = (p.customerPhone ?? '').replace(/\D/g, '');
    if (phone.startsWith('0')) phone = '90' + phone.slice(1);
    else if (phone.length === 10) phone = '90' + phone;
    const first = p.customerName.split(' ')[0] ?? '';
    const text = `Merhaba ${first}, ${p.name} paketinizin seanslarını ve ödemelerinizi buradan takip edebilirsiniz: ${packageTrackUrl(p.publicToken)}`;
    return `https://wa.me/${phone}?text=${encodeURIComponent(text)}`;
  }

  private mutate(call: ReturnType<PackageService['getPackage']>, message: string, after?: () => void): void {
    this.busy.set(true);
    this.error.set('');
    this.notice.set('');
    call.subscribe({
      next: r => {
        this.busy.set(false);
        if (!r.success) return;
        this.apply(r.data);
        this.notice.set(message);
        after?.();
      },
      error: err => { this.busy.set(false); this.error.set(err?.error?.message || 'İşlem yapılamadı.'); },
    });
  }
}
