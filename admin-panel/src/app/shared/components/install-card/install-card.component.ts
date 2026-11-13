import { Component, computed, signal } from '@angular/core';
import { PwaInstallService } from '../../../core/services/pwa-install.service';

const DISMISS_KEY = 'pwa_install_dismissed_at';
const DISMISS_DAYS = 30;

/**
 * "ayarlıyo'yu telefonunuza kurun" kartı — ana sayfada, yalnızca telefonda,
 * kurulu değilse ve son 30 günde kapatılmadıysa görünür.
 */
@Component({
  selector: 'app-install-card',
  standalone: true,
  template: `
    @if (visible()) {
      <div class="ic" role="region" aria-label="Uygulamayı kur">
        <img src="icon-maskable-192.png" alt="" width="44" height="44">
        <div class="ic-body">
          <strong>ayarlıyo'yu telefonunuza kurun</strong>
          @if (pwa.mode === 'android') {
            <span>Ana ekranda simgesi olur, tek dokunuşla açılır; tarayıcı çubuğu olmadan tam ekran.</span>
            <div class="ic-actions">
              <button type="button" class="ic-go" (click)="install()">Uygulamayı yükle</button>
              <button type="button" class="ic-later" (click)="dismiss()">Şimdi değil</button>
            </div>
          } @else {
            <span>Safari'de alttaki <b>Paylaş</b> simgesine
              <svg class="ic-share" viewBox="0 0 24 24" aria-label="Paylaş simgesi" role="img"><path d="M12 3v12M8 7l4-4 4 4M6 11v8a2 2 0 0 0 2 2h8a2 2 0 0 0 2-2v-8" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/></svg>
              dokunun, ardından <b>Ana Ekrana Ekle</b>'yi seçin.</span>
            <div class="ic-actions">
              <button type="button" class="ic-later" (click)="dismiss()">Anladım</button>
            </div>
          }
        </div>
        <button type="button" class="ic-x" aria-label="Kapat" (click)="dismiss()">✕</button>
      </div>
    }
  `,
  styles: [`
    .ic {
      position: relative; display: flex; gap: 12px; align-items: flex-start;
      margin-bottom: 16px; padding: 14px 40px 14px 14px; border-radius: 16px;
      background: #111827; color: #fff; font-family: 'Inter', sans-serif;
    }
    .ic img { border-radius: 12px; flex: none; }
    .ic-body { display: grid; gap: 4px; min-width: 0; }
    .ic-body strong { font-size: 14.5px; }
    .ic-body span { font-size: 13px; color: #d1d5db; line-height: 1.45; }
    .ic-body b { color: #fff; }
    .ic-share { width: 15px; height: 15px; vertical-align: -2px; color: #fff; }
    .ic-actions { display: flex; gap: 8px; margin-top: 8px; flex-wrap: wrap; }
    .ic-go { height: 36px; padding: 0 14px; border: none; border-radius: 10px; background: #fff; color: #111827;
             font: 700 13.5px 'Inter', sans-serif; cursor: pointer; }
    .ic-later { height: 36px; padding: 0 12px; border: 1px solid #374151; border-radius: 10px; background: none; color: #e5e7eb;
                font: 600 13px 'Inter', sans-serif; cursor: pointer; }
    .ic-x { position: absolute; top: 8px; right: 8px; width: 28px; height: 28px; border: none; border-radius: 8px;
            background: none; color: #9ca3af; cursor: pointer; font-size: 13px; }
  `],
})
export class InstallCardComponent {
  private readonly dismissed = signal(InstallCardComponent.recentlyDismissed());
  private readonly isPhone = window.matchMedia?.('(max-width: 768px)').matches ?? false;

  readonly visible = computed(() =>
    this.isPhone && !this.dismissed() && !this.pwa.installed() &&
    (this.pwa.canPrompt() || this.pwa.mode === 'ios'));

  constructor(readonly pwa: PwaInstallService) {}

  async install(): Promise<void> {
    const ok = await this.pwa.prompt();
    if (!ok) this.dismiss();
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
