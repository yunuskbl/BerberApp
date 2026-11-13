import { Injectable, signal } from '@angular/core';

/** Chrome/Android'in "beforeinstallprompt" olayı (standart tiplerde yok). */
interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: 'accepted' | 'dismissed' }>;
}

export type InstallMode = 'android' | 'ios' | 'none';

/**
 * Panelin telefona kurulması.
 *
 * Android/Chrome kurulabilir sayfada bir kez "beforeinstallprompt" olayı
 * yayar; o an yakalanmazsa kurulum düğmesi gösterilemez. Olay uygulama
 * açılır açılmaz gelebildiği için bu servis kök bileşende oluşturulur.
 * iPhone'da (Safari) böyle bir olay yok: kurulum Paylaş → Ana Ekrana Ekle
 * ile yapılır, o yüzden yalnızca rehber gösterilir.
 */
@Injectable({ providedIn: 'root' })
export class PwaInstallService {
  private deferred: BeforeInstallPromptEvent | null = null;
  readonly canPrompt = signal(false);
  readonly installed = signal(PwaInstallService.isStandalone());

  constructor() {
    window.addEventListener('beforeinstallprompt', e => {
      e.preventDefault();             // tarayıcının kendi küçük çubuğu yerine bizim kartımız
      this.deferred = e as BeforeInstallPromptEvent;
      this.canPrompt.set(true);
    });
    window.addEventListener('appinstalled', () => {
      this.deferred = null;
      this.canPrompt.set(false);
      this.installed.set(true);
    });
  }

  /** Bu cihazda hangi kurulum yolu var. */
  get mode(): InstallMode {
    if (this.installed()) return 'none';
    if (this.canPrompt()) return 'android';
    return PwaInstallService.isIosSafari() ? 'ios' : 'none';
  }

  async prompt(): Promise<boolean> {
    if (!this.deferred) return false;
    const ev = this.deferred;
    this.deferred = null;
    this.canPrompt.set(false);
    await ev.prompt();
    const choice = await ev.userChoice;
    return choice.outcome === 'accepted';
  }

  /** Ana ekrandan açıldı mı (uygulama gibi, tarayıcı çubuğu olmadan)? */
  static isStandalone(): boolean {
    return window.matchMedia?.('(display-mode: standalone)').matches === true
      || (navigator as any).standalone === true;
  }

  private static isIosSafari(): boolean {
    const ua = navigator.userAgent;
    const ios = /iPhone|iPad|iPod/i.test(ua) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    // iOS'taki Chrome/Firefox da ana ekrana ekleyebiliyor ama menüleri farklı;
    // rehber Safari'nin Paylaş düğmesini anlattığı için yalnız Safari'de.
    return ios && /Safari/i.test(ua) && !/CriOS|FxiOS|EdgiOS/i.test(ua);
  }
}
