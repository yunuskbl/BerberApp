import { Component, OnInit, computed, signal } from '@angular/core';
import { PushService } from '../../../core/services/push.service';
import { PwaInstallService } from '../../../core/services/pwa-install.service';

const DISMISS_KEY = 'push_prompt_dismissed_at';
const DISMISS_DAYS = 14;

/**
 * Ana sayfada bir kez "bildirimleri açın" daveti.
 *
 * Telefonda yalnızca uygulama ana ekrana kurulduktan sonra çıkar: kurulum
 * kartıyla üst üste binmesin, iPhone'da zaten kurulmadan çalışmıyor.
 * Bilgisayarda (salondaki kasa bilgisayarı) doğrudan çıkar. İzin daha önce
 * sorulduysa (verildi ya da reddedildi) bir daha gösterilmez.
 */
@Component({
  selector: 'app-push-prompt',
  standalone: true,
  template: `
    @if (visible()) {
      <div class="pp" role="region" aria-label="Bildirimler">
        <div class="pp-icon">🔔</div>
        <div class="pp-body">
          <strong>Yeni randevular telefonunuza gelsin</strong>
          <span>Müşteri randevu aldığında, iptal ettiğinde ya da seansını onayladığında anında haberiniz olsun.</span>
          @if (error(); as e) { <span class="pp-err">{{ e }}</span> }
          <div class="pp-actions">
            <button type="button" class="pp-go" [disabled]="push.busy()" (click)="enable()">
              {{ push.busy() ? 'Açılıyor…' : 'Bildirimleri aç' }}
            </button>
            <button type="button" class="pp-later" (click)="dismiss()">Şimdi değil</button>
          </div>
        </div>
        <button type="button" class="pp-x" aria-label="Kapat" (click)="dismiss()">✕</button>
      </div>
    }
    @if (done()) {
      <div class="pp pp-done" role="status">✓ Bildirimler bu cihazda açıldı.</div>
    }
  `,
  styles: [`
    .pp {
      position: relative; display: flex; gap: 12px; align-items: flex-start;
      margin-bottom: 16px; padding: 14px 40px 14px 14px; border-radius: 16px;
      background: #fff; border: 1px solid #e5e7eb; box-shadow: 0 1px 2px rgba(0,0,0,.04);
      font-family: 'Inter', sans-serif;
    }
    .pp-icon { font-size: 22px; line-height: 1; width: 40px; height: 40px; border-radius: 12px; background: #fef3c7;
               display: grid; place-items: center; flex: none; }
    .pp-body { display: grid; gap: 4px; min-width: 0; }
    .pp-body strong { font-size: 14.5px; color: #111827; }
    .pp-body span { font-size: 13px; color: #6b7280; line-height: 1.45; }
    .pp-body .pp-err { color: #b91c1c; }
    .pp-actions { display: flex; gap: 8px; margin-top: 8px; flex-wrap: wrap; }
    .pp-go { height: 36px; padding: 0 14px; border: none; border-radius: 10px; background: #111827; color: #fff;
             font: 700 13.5px 'Inter', sans-serif; cursor: pointer; }
    .pp-go:disabled { opacity: .6; }
    .pp-later { height: 36px; padding: 0 12px; border: 1px solid #e5e7eb; border-radius: 10px; background: none; color: #4b5563;
                font: 600 13px 'Inter', sans-serif; cursor: pointer; }
    .pp-x { position: absolute; top: 8px; right: 8px; width: 28px; height: 28px; border: none; border-radius: 8px;
            background: none; color: #9ca3af; cursor: pointer; font-size: 13px; }
    .pp-done { padding: 12px 14px; color: #047857; font-weight: 600; font-size: 14px; }
  `],
})
export class PushPromptComponent implements OnInit {
  private readonly dismissed = signal(PushPromptComponent.recentlyDismissed());
  private readonly isPhone = window.matchMedia?.('(max-width: 768px)').matches ?? false;
  readonly error = signal<string | null>(null);
  readonly done = signal(false);

  readonly visible = computed(() =>
    !this.dismissed() && !this.done() &&
    this.push.state() === 'off' &&
    typeof Notification !== 'undefined' && Notification.permission === 'default' &&
    (!this.isPhone || this.pwa.installed()));

  constructor(readonly push: PushService, private pwa: PwaInstallService) {}

  ngOnInit(): void {
    this.push.init();
  }

  async enable(): Promise<void> {
    this.error.set(null);
    const r = await this.push.enable();
    if (r.ok) {
      this.done.set(true);
      setTimeout(() => this.done.set(false), 4000);
    } else if (r.message) {
      this.error.set(r.message);
    } else {
      this.dismiss();   // izin penceresinde "İzin verme" dendi
    }
  }

  dismiss(): void {
    try { localStorage.setItem(DISMISS_KEY, String(Date.now())); } catch { /* yok */ }
    this.dismissed.set(true);
  }

  private static recentlyDismissed(): boolean {
    try {
      const at = Number(localStorage.getItem(DISMISS_KEY) || 0);
      return at > 0 && Date.now() - at < DISMISS_DAYS * 86_400_000;
    } catch { return false; }
  }
}
