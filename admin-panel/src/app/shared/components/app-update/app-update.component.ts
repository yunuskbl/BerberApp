import { Component, OnDestroy, OnInit, signal } from '@angular/core';
import { SwUpdate, VersionReadyEvent } from '@angular/service-worker';
import { filter } from 'rxjs';

/**
 * "Yeni sürüm hazır" şeridi.
 *
 * Telefona kurulan panel uygulama kabuğunu service worker önbelleğinden
 * açıyor; dağıtımdan sonra yeni sürüm arka planda iner ama sayfa
 * yenilenmeden devreye girmez. Bu şerit olmasaydı kullanıcı yeni özellikleri
 * ancak uygulamayı iki kez kapatıp açınca görürdü. Ayrıca uygulama ön plana
 * her döndüğünde ve yarım saatte bir güncelleme kontrol edilir.
 */
@Component({
  selector: 'app-update',
  standalone: true,
  template: `
    @if (ready()) {
      <div class="upd" role="status">
        <span>Yeni sürüm hazır.</span>
        <button type="button" class="upd-go" (click)="reload()">Yenile</button>
        <button type="button" class="upd-x" aria-label="Kapat" (click)="ready.set(false)">✕</button>
      </div>
    }
  `,
  styles: [`
    .upd {
      position: fixed; left: 50%; transform: translateX(-50%); z-index: 10000;
      bottom: calc(76px + env(safe-area-inset-bottom, 0px));
      display: flex; align-items: center; gap: 10px;
      padding: 10px 10px 10px 16px; border-radius: 14px;
      background: #111827; color: #fff; box-shadow: 0 12px 32px rgba(0,0,0,.25);
      font: 600 14px 'Inter', sans-serif; white-space: nowrap;
    }
    .upd-go {
      height: 32px; padding: 0 14px; border: none; border-radius: 9px;
      background: #fff; color: #111827; font: inherit; font-size: 13px; cursor: pointer;
    }
    .upd-x { border: none; background: none; color: #9ca3af; font-size: 14px; cursor: pointer; padding: 4px; }
    @media (min-width: 769px) { .upd { bottom: 24px; } }
  `],
})
export class AppUpdateComponent implements OnInit, OnDestroy {
  readonly ready = signal(false);
  private timer: ReturnType<typeof setInterval> | null = null;
  private readonly onVisible = () => { if (document.visibilityState === 'visible') this.check(); };

  constructor(private sw: SwUpdate) {}

  ngOnInit(): void {
    if (!this.sw.isEnabled) return;
    this.sw.versionUpdates
      .pipe(filter((e): e is VersionReadyEvent => e.type === 'VERSION_READY'))
      .subscribe(() => this.ready.set(true));
    // Önbellekteki sürüm bozulduysa (ör. sunucudan silinmiş dosyalar) temiz yükle.
    this.sw.unrecoverable.subscribe(() => document.location.reload());
    this.timer = setInterval(() => this.check(), 30 * 60 * 1000);
    document.addEventListener('visibilitychange', this.onVisible);
  }

  ngOnDestroy(): void {
    if (this.timer) clearInterval(this.timer);
    document.removeEventListener('visibilitychange', this.onVisible);
  }

  reload(): void {
    this.sw.activateUpdate().finally(() => document.location.reload());
  }

  private check(): void {
    this.sw.checkForUpdate().catch(() => { /* çevrimdışı: sonra tekrar denenir */ });
  }
}
